using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectronicJova.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

        // GET: /Customer/Order/Details/{orderId}
        public async Task<IActionResult> Details(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                u => u.Id == orderId,
                includeProperties: "ApplicationUser");

            if (orderHeader == null) return NotFound();

            // Ownership check — solo el dueño puede ver su orden
            if (orderHeader.ApplicationUserId != userId)
                return Forbid();

            var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(
                u => u.OrderHeaderId == orderId,
                includeProperties: "Product");

            ViewBag.OrderDetails = orderDetails;
            return View(orderHeader);
        }
    }
}
