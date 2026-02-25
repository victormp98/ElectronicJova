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

        public async Task<IActionResult> Index(int? pageNumber)
        {
            // Retrieve queryable for products
            IQueryable<Product> productQuery = _unitOfWork.Product.GetQueryable(includeProperties: "Category", tracked: false);

            int pageSize = 8;
            var paginatedProducts = await PaginatedList<Product>.CreateAsync(productQuery, pageNumber ?? 1, pageSize);

            // FS-05: Initialize CartItemCount from DB if session is missing (e.g., after login)
            if (User.Identity?.IsAuthenticated == true && HttpContext.Session.GetInt32("CartItemCount") == null)
            {
                var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    var cartCount = (await _unitOfWork.ShoppingCart.GetAllAsync(c => c.ApplicationUserId == userId)).Count();
                    HttpContext.Session.SetInt32("CartItemCount", cartCount);
                }
            }

            return View(paginatedProducts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Home/Error/{statusCode?}")]
        public IActionResult Error(int? statusCode)
        {
            if (statusCode == 404)
            {
                // Log 404 if needed
            }
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, StatusCode = statusCode });
        }

        public async Task<IActionResult> Search(string? searchString, int? categoryId, int? pageNumber)
        {
            _logger.LogInformation("Search initiated for: {SearchString}, CategoryId: {CategoryId}, Page: {PageNumber}", searchString, categoryId, pageNumber);

            // Start with a queryable object for efficient filtering
            IQueryable<Product> productQuery = _unitOfWork.Product.GetQueryable(includeProperties: "Category", tracked: false);

            if (!string.IsNullOrEmpty(searchString))
            {
                productQuery = productQuery.Where(p => p.Title.Contains(searchString) ||
                                                    p.Description.Contains(searchString));
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

        public async Task<IActionResult> Details(int id)
        {
            var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(u => u.Id == id, includeProperties: "Category");

            if (product == null)
            {
                return NotFound();
            }

            DetailsVM detailsVM = new()
            {
                Product = product,
                Count = 1,
                ProductOptions = await _unitOfWork.ProductOption.GetAllAsync(u => u.ProductId == id)
            };

            // Verificar si el producto está en favoritos del usuario
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            if (claimsIdentity?.IsAuthenticated == true)
            {
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var wishlistItem = await _unitOfWork.Wishlist.GetFirstOrDefaultAsync(
                        u => u.ApplicationUserId == userId && u.ProductId == id
                    );
                    detailsVM.IsFavorite = wishlistItem != null;
                }
            }

            return View(detailsVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // User must be logged in to add to cart
        public async Task<IActionResult> Details(DetailsVM detailsVM, List<int>? selectedOptions, string? specialNotes)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            // B-05: Reload product from DB to prevent tampered hidden-field attacks
            var productFromDb = await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Id == detailsVM.Product.Id);
            if (productFromDb == null)
            {
                return NotFound();
            }

            // Almacenar opciones seleccionadas como JSON
            string? optionsJson = null;
            if (selectedOptions != null && selectedOptions.Any())
            {
                var options = await _unitOfWork.ProductOption.GetAllAsync(o => selectedOptions.Contains(o.Id));
                optionsJson = System.Text.Json.JsonSerializer.Serialize(options.Select(o => new { o.Name, o.Value, o.AdditionalPrice }));
            }

            ShoppingCart cartFromDb = await _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(
                u => u.ApplicationUserId == userId && u.ProductId == detailsVM.Product.Id && u.SelectedOptions == optionsJson
            );

            if (cartFromDb != null)
            {
                // Si ya existe el mismo producto con las mismas opciones, solo aumentar cantidad
                cartFromDb.Count += detailsVM.Count;
                if (!string.IsNullOrEmpty(specialNotes))
                {
                    cartFromDb.SpecialNotes = (cartFromDb.SpecialNotes ?? "") + " | " + specialNotes;
                }
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            else
            {
                // Agregar nuevo registro
                ShoppingCart newCart = new()
                {
                    ApplicationUserId = userId,
                    ProductId = detailsVM.Product.Id,
                    Count = detailsVM.Count,
                    SelectedOptions = optionsJson,
                    SpecialNotes = specialNotes
                };
                await _unitOfWork.ShoppingCart.AddAsync(newCart);
            }

            await _unitOfWork.SaveAsync();

            // Update session with cart item count
            var cartItemCount = (await _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == userId)).Count();
            HttpContext.Session.SetInt32("CartItemCount", cartItemCount);

            TempData["success"] = "Producto agregado al carrito con éxito.";
            return RedirectToAction(nameof(Details), new { id = detailsVM.Product.Id });
        }
    }
}
