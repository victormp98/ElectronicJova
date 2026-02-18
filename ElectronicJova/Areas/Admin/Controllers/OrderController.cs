using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Models.ViewModels;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Stripe;
using ElectronicJova.Hubs;

namespace ElectronicJova.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<OrderStatusHub> _hubContext;

        [BindProperty]
        public OrderDetailsVM OrderDetailsVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork, IHubContext<OrderStatusHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
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
            orderHEaderFromDb.PhoneNumber = OrderDetailsVM.OrderHeader.PhoneNumber;
            orderHEaderFromDb.StreetAddress = OrderDetailsVM.OrderHeader.StreetAddress;
            orderHEaderFromDb.City = OrderDetailsVM.OrderHeader.City;
            orderHEaderFromDb.State = OrderDetailsVM.OrderHeader.State;
            orderHEaderFromDb.PostalCode = OrderDetailsVM.OrderHeader.PostalCode;

            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Detalles del pedido actualizados.";
            return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartProcessing()
        {
            var orderHEaderFromDb = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == OrderDetailsVM.OrderHeader.Id);
            orderHEaderFromDb.OrderStatus = SD.StatusInProcess;
            orderHEaderFromDb.OrderStatusValue = (int)SD.OrderStatus.Processing;
            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();

            // SignalR: notificar al cliente en tiempo real
            await _hubContext.Clients.Group($"order-{orderHEaderFromDb.Id}")
                .SendAsync("OrderStatusUpdated", SD.StatusInProcess,
                    SD.GetOrderStatusLabel(SD.StatusInProcess),
                    SD.GetOrderStatusIcon(SD.StatusInProcess));

            TempData["success"] = "Pedido en proceso.";
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
            orderHEaderFromDb.OrderStatusValue = (int)SD.OrderStatus.Shipped;
            orderHEaderFromDb.ShippingDate = DateTime.Now;
            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();

            // SignalR: notificar al cliente en tiempo real
            await _hubContext.Clients.Group($"order-{orderHEaderFromDb.Id}")
                .SendAsync("OrderStatusUpdated", SD.StatusShipped,
                    SD.GetOrderStatusLabel(SD.StatusShipped),
                    SD.GetOrderStatusIcon(SD.StatusShipped));

            TempData["success"] = "Pedido marcado como enviado.";
            return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDelivered()
        {
            var orderHEaderFromDb = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == OrderDetailsVM.OrderHeader.Id);
            orderHEaderFromDb.OrderStatus = SD.StatusDelivered;
            orderHEaderFromDb.OrderStatusValue = (int)SD.OrderStatus.Delivered;
            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();

            // SignalR: notificar al cliente en tiempo real
            await _hubContext.Clients.Group($"order-{orderHEaderFromDb.Id}")
                .SendAsync("OrderStatusUpdated", SD.StatusDelivered,
                    SD.GetOrderStatusLabel(SD.StatusDelivered),
                    SD.GetOrderStatusIcon(SD.StatusDelivered));

            TempData["success"] = "Pedido marcado como entregado.";
            return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder()
        {
            var orderHEaderFromDb = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == OrderDetailsVM.OrderHeader.Id);

            // Si el pago fue aprobado, emitir reembolso en Stripe antes de cancelar
            if (orderHEaderFromDb.PaymentStatus == SD.PaymentStatusApproved
                && !string.IsNullOrEmpty(orderHEaderFromDb.PaymentIntentId))
            {
                try
                {
                    var refundOptions = new RefundCreateOptions
                    {
                        PaymentIntent = orderHEaderFromDb.PaymentIntentId,
                        Reason = RefundReasons.RequestedByCustomer
                    };
                    var refundService = new RefundService();
                    await refundService.CreateAsync(refundOptions);
                    orderHEaderFromDb.PaymentStatus = SD.PaymentStatusRefunded;
                    orderHEaderFromDb.PaymentStatusValue = (int)SD.PaymentStatus.Refunded;
                }
                catch (StripeException ex)
                {
                    TempData["error"] = $"Error al procesar el reembolso en Stripe: {ex.Message}";
                    return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
                }
            }

            orderHEaderFromDb.OrderStatus = SD.StatusCancelled;
            orderHEaderFromDb.OrderStatusValue = (int)SD.OrderStatus.Cancelled;
            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();

            // SignalR: notificar al cliente en tiempo real
            await _hubContext.Clients.Group($"order-{orderHEaderFromDb.Id}")
                .SendAsync("OrderStatusUpdated", SD.StatusCancelled,
                    SD.GetOrderStatusLabel(SD.StatusCancelled),
                    SD.GetOrderStatusIcon(SD.StatusCancelled));

            TempData["success"] = "Orden cancelada exitosamente.";
            return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
        }
    }
}
