using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Models.ViewModels;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectronicJova.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)] // Only Admin can manage orders
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty] // Automatically bind OrderDetailsVM to requests
        public OrderDetailsVM OrderDetailsVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<OrderHeader> orderHeaders;
            orderHeaders = await _unitOfWork.OrderHeader.GetAllAsync(includeProperties: "ApplicationUser");
            return View(orderHeaders);
        }

        public async Task<IActionResult> Details(int orderId)
        {
            OrderDetailsVM = new OrderDetailsVM()
            {
                OrderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = await _unitOfWork.OrderDetail.GetAllAsync(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderDetailsVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderDetail()
        {
            var orderHEaderFromDb = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == OrderDetailsVM.OrderHeader.Id);
            orderHEaderFromDb.Name = OrderDetailsVM.OrderHeader.Name;
            orderHEaderFromDb.PhoneNumber = OrderDetailsVM.OrderHeader.PhoneNumber; // Corrected
            orderHEaderFromDb.StreetAddress = OrderDetailsVM.OrderHeader.StreetAddress;
            orderHEaderFromDb.City = OrderDetailsVM.OrderHeader.City;
            orderHEaderFromDb.State = OrderDetailsVM.OrderHeader.State;
            orderHEaderFromDb.PostalCode = OrderDetailsVM.OrderHeader.PostalCode;

            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartProcessing()
        {
            var orderHEaderFromDb = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == OrderDetailsVM.OrderHeader.Id);
            orderHEaderFromDb.OrderStatus = SD.StatusInProcess;
            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Order Status Updated to In Process.";
            return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder()
        {
            var orderHEaderFromDb = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == OrderDetailsVM.OrderHeader.Id);
            orderHEaderFromDb.TrackingNumber = OrderDetailsVM.OrderHeader.TrackingNumber;
            orderHEaderFromDb.Carrier = OrderDetailsVM.OrderHeader.Carrier;
            orderHEaderFromDb.OrderStatus = SD.StatusShipped;
            orderHEaderFromDb.ShippingDate = DateTime.Now;
            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Order Status Updated to Shipped.";
            return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder()
        {
            var orderHEaderFromDb = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == OrderDetailsVM.OrderHeader.Id);
            orderHEaderFromDb.OrderStatus = SD.StatusCancelled;
            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Order Status Updated to Cancelled.";
            return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
        }
    }
}
