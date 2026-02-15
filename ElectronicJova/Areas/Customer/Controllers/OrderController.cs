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

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            IEnumerable<OrderHeader> orderHeaders = await _unitOfWork.OrderHeader.GetAllAsync(
                u => u.ApplicationUserId == userId,
                includeProperties: "ApplicationUser"
            );
            return View(orderHeaders);
        }

        public async Task<IActionResult> Details(int orderId)
        {
            OrderHeader orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(
                u => u.Id == orderId,
                includeProperties: "ApplicationUser,OrderDetails.Product"
            );
            return View(orderHeader);
        }
    }
}
