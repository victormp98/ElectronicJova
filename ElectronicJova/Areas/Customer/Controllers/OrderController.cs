using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ElectronicJova.Models.ViewModels;
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
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
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
            var statusLogs = await _unitOfWork.OrderStatusLog.GetAllAsync(
                u => u.OrderHeaderId == id);

            ViewBag.OrderDetails = orderDetails;
            ViewBag.StatusLogs = statusLogs.OrderByDescending(l => l.ChangedAt);
            return View(orderHeader);
        }

        // GET: /Customer/Order/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var vm = new ProfileVM
            {
                Name = string.IsNullOrWhiteSpace(user.Name) ? DeriveFallbackName(user) : user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                StreetAddress = user.StreetAddress,
                City = user.City,
                State = user.State,
                PostalCode = user.PostalCode
            };

            return View(vm);
        }

        // POST: /Customer/Order/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(model.Name) && Request.HasFormContentType)
            {
                model.Name = Request.Form["Name"].ToString();
                model.PhoneNumber = Request.Form["PhoneNumber"].ToString();
                model.StreetAddress = Request.Form["StreetAddress"].ToString();
                model.City = Request.Form["City"].ToString();
                model.State = Request.Form["State"].ToString();
                model.PostalCode = Request.Form["PostalCode"].ToString();
            }

            model.Name = (model.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.Name = string.IsNullOrWhiteSpace(user.Name) ? DeriveFallbackName(user) : user.Name;
                ModelState.Remove(nameof(ProfileVM.Name));
                _logger.LogWarning("Profile name was empty on POST. Applied fallback name for UserId={UserId}", user.Id);
            }

            _logger.LogInformation(
                "Profile update attempt. UserId={UserId}, NameLen={NameLen}, ModelStateValid={ModelStateValid}",
                user.Id,
                model.Name.Length,
                ModelState.IsValid);

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;
                foreach (var kv in ModelState.Where(m => m.Value?.Errors.Count > 0))
                {
                    _logger.LogWarning("Profile validation error field={Field} errors={Errors}",
                        kv.Key,
                        string.Join(" | ", kv.Value!.Errors.Select(e => e.ErrorMessage)));
                }
                return View(model);
            }

            user.Name = MergeOrKeep(user.Name, model.Name);
            user.PhoneNumber = MergeOrKeep(user.PhoneNumber, model.PhoneNumber);
            user.StreetAddress = MergeOrKeep(user.StreetAddress, model.StreetAddress);
            user.City = MergeOrKeep(user.City, model.City);
            user.State = MergeOrKeep(user.State, model.State);
            user.PostalCode = MergeOrKeep(user.PostalCode, model.PostalCode);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user); // Force cookie refresh to update visible data
                TempData["success"] = "Perfil actualizado correctamente.";
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    _logger.LogWarning("Profile update failed. UserId={UserId}, Error={Error}", user.Id, error.Description);
                }
                model.Email = user.Email;
                return View(model);
            }

            return RedirectToAction(nameof(Profile));
        }

        private static string? MergeOrKeep(string? currentValue, string? incomingValue)
        {
            if (string.IsNullOrWhiteSpace(incomingValue))
            {
                return currentValue;
            }

            return incomingValue.Trim();
        }

        private static string DeriveFallbackName(ApplicationUser user)
        {
            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                var userNamePart = user.UserName.Split('@')[0].Trim();
                if (!string.IsNullOrWhiteSpace(userNamePart))
                {
                    return userNamePart;
                }
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var emailPart = user.Email.Split('@')[0].Trim();
                if (!string.IsNullOrWhiteSpace(emailPart))
                {
                    return emailPart;
                }
            }

            return "Usuario";
        }
    }
}
