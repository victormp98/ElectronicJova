using ElectronicJova.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using ElectronicJova.Data.Repository; // Added for IUnitOfWork
using ElectronicJova.Models.ViewModels; // Added for SearchVM
using ElectronicJova.Utilities; // Added for PaginatedList
using Microsoft.AspNetCore.Mvc.Rendering; // Added for SelectListItem

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ElectronicJova.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork; // Added for database access

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            // Retrieve products from the database, including their Category for display
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category");
            return View(productList);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Search(string? searchString, int? categoryId, int? pageNumber)
        {
            _logger.LogInformation("Search initiated for: {SearchString}, CategoryId: {CategoryId}, Page: {PageNumber}", searchString, categoryId, pageNumber);

            // Start with a queryable object for efficient filtering
            IQueryable<Product> productQuery = _unitOfWork.Product.GetQueryable(includeProperties: "Category", tracked: false);

            if (!string.IsNullOrEmpty(searchString))
            {
                productQuery = productQuery.Where(p => p.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                                                    p.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productQuery = productQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            int pageSize = 8; // Number of products per page
            var paginatedProducts = await PaginatedList<Product>.CreateAsync(productQuery, pageNumber ?? 1, pageSize);

            var searchVM = new SearchVM
            {
                Products = paginatedProducts,
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                SearchString = searchString,
                CategoryId = categoryId
            };

            return View("Search", searchVM);
        }

        public async Task<IActionResult> Details(int productId)
        {
            DetailsVM detailsVM = new()
            {
                Product = await _unitOfWork.Product.GetFirstOrDefaultAsync(u => u.Id == productId, includeProperties: "Category"),
                Count = 1
            };

            if (detailsVM.Product == null)
            {
                return NotFound();
            }

            return View(detailsVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // User must be logged in to add to cart
        public async Task<IActionResult> Details(DetailsVM detailsVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCart cartFromDb = await _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(
                u => u.ApplicationUserId == userId && u.ProductId == detailsVM.Product.Id
            );

            if (cartFromDb != null)
            {
                // Shopping cart for user already exists, just update count
                cartFromDb.Count += detailsVM.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            else
            {
                // Add new cart record
                ShoppingCart newCart = new()
                {
                    ApplicationUserId = userId,
                    ProductId = detailsVM.Product.Id,
                    Count = detailsVM.Count
                };
                await _unitOfWork.ShoppingCart.AddAsync(newCart);
            }
            await _unitOfWork.SaveAsync();

            // Update session with cart item count
            var cartItemCount = (await _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == userId)).Count();
            HttpContext.Session.SetInt32("CartItemCount", cartItemCount);

            TempData["success"] = "Product added to cart successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
