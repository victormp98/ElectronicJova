using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectronicJova.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = SD.Role_Customer)]
    public class WishlistController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public WishlistController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            var wishlistItems = await _unitOfWork.Wishlist.GetAllAsync(
                w => w.ApplicationUserId == userId,
                includeProperties: "Product,Product.Category"
            );

            return View(wishlistItems);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Json(new { success = false, message = "Debe iniciar sesión" });
            }

            var existing = await _unitOfWork.Wishlist.GetFirstOrDefaultAsync(
                w => w.ApplicationUserId == userId && w.ProductId == productId
            );

            if (existing == null)
            {
                // Agregar a favoritos
                var wishlist = new Wishlist
                {
                    ApplicationUserId = userId,
                    ProductId = productId,
                    AddedDate = DateTime.Now
                };
                await _unitOfWork.Wishlist.AddAsync(wishlist);
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, action = "added" });
            }
            else
            {
                // Quitar de favoritos
                _unitOfWork.Wishlist.Remove(existing);
                await _unitOfWork.SaveAsync();
                return Json(new { success = true, action = "removed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var wishlist = await _unitOfWork.Wishlist.GetFirstOrDefaultAsync(w => w.Id == id);
            if (wishlist == null)
                return Json(new { success = false, message = "No encontrado" });

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Json(new { success = false, message = "Debe iniciar sesión" });
            }
            if (wishlist.ApplicationUserId != userId)
                return Json(new { success = false, message = "No autorizado" });

            _unitOfWork.Wishlist.Remove(wishlist);
            await _unitOfWork.SaveAsync();
            return Json(new { success = true });
        }
    }
}
