using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectronicJova.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public OrderController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Customer/Order — Historial de pedidos del cliente
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var orders = await _unitOfWork.OrderHeader.GetAllAsync(
                u => u.ApplicationUserId == userId,
                includeProperties: "ApplicationUser");

            return View(orders.OrderByDescending(o => o.OrderDate));
        }

        // GET: /Customer/Order/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                u => u.Id == id,
                includeProperties: "ApplicationUser");

            if (orderHeader == null) return NotFound();

            // Ownership check — solo el dueño puede ver su orden
            if (orderHeader.ApplicationUserId != userId)
                return Forbid();

            var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(
                u => u.OrderHeaderId == id,
                includeProperties: "Product");

            ViewBag.OrderDetails = orderDetails;
            return View(orderHeader);
        }

        // GET: /Customer/Order/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            return View(user);
        }

        // POST: /Customer/Order/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            user.Name = model.Name;
            user.PhoneNumber = model.PhoneNumber;
            user.StreetAddress = model.StreetAddress;
            user.City = model.City;
            user.State = model.State;
            user.PostalCode = model.PostalCode;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user); // Force cookie refresh to update visible data
                TempData["success"] = "Perfil actualizado correctamente.";
            }
            else
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            return RedirectToAction(nameof(Profile));
        }
    }
}
