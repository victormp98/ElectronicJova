using ElectronicJova.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ElectronicJova.Data.Repository; // Added for IUnitOfWork

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

        public IActionResult Search(string? searchString)
        {
            _logger.LogInformation("Search initiated for: {SearchString}", searchString);
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category");

            if (!string.IsNullOrEmpty(searchString))
            {
                productList = productList.Where(p => p.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                                                    p.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            // For now, display the filtered products on the existing Index view.
            // A dedicated search results page can be created in the next sub-task.
            return View("Search", productList);
        }
    }
}
