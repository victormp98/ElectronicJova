using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Models.ViewModels;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Stripe;
using ElectronicJova.Hubs;
using Microsoft.AspNetCore.Identity.UI.Services; // Added for IEmailSender

namespace ElectronicJova.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender; // Added Email Sender

        [BindProperty]
        public OrderDetailsVM OrderDetailsVM { get; set; } = new();

        public OrderController(IUnitOfWork unitOfWork, IHubContext<OrderStatusHub> hubContext, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index(int? pageNumber)
        {
            IQueryable<OrderHeader> orderHeaderQuery = _unitOfWork.OrderHeader.GetQueryable(includeProperties: "ApplicationUser", tracked: false);

            int pageSize = 10;
            var paginatedOrders = await PaginatedList<OrderHeader>.CreateAsync(orderHeaderQuery, pageNumber ?? 1, pageSize);

            return View(paginatedOrders);
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
            if (orderHEaderFromDb == null) return NotFound();

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
            if (orderHEaderFromDb == null) return NotFound();

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
            if (orderHEaderFromDb == null) return NotFound();

            orderHEaderFromDb.TrackingNumber = OrderDetailsVM.OrderHeader.TrackingNumber;
            orderHEaderFromDb.Carrier = OrderDetailsVM.OrderHeader.Carrier;
            orderHEaderFromDb.OrderStatus = SD.StatusShipped;
            orderHEaderFromDb.OrderStatusValue = (int)SD.OrderStatus.Shipped;
            orderHEaderFromDb.ShippingDate = DateTime.Now;
            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();

                .SendAsync("OrderStatusUpdated", SD.StatusShipped,
                    SD.GetOrderStatusLabel(SD.StatusShipped),
                    SD.GetOrderStatusIcon(SD.StatusShipped));

            // EMAIL NOTIFICATION: Pedido Enviado
            try 
            {
                string emailSubject = $"Tu pedido #{orderHEaderFromDb.Id} está en camino - ElectronicJova";
                string emailBody = $@"
                    <div style='font-family: Arial, sans-serif; color: #333;'>
                        <h2 style='color: #00d4ff;'>¡Tu pedido ha sido enviado!</h2>
                        <p>Hola <strong>{orderHEaderFromDb.Name}</strong>,</p>
                        <p>Hemos enviado tus productos. Aquí están los detalles:</p>
                        <p><strong>Transportista:</strong> {orderHEaderFromDb.Carrier}</p>
                        <p><strong>Número de Rastreo:</strong> {orderHEaderFromDb.TrackingNumber}</p>
                        <hr style='border: 1px solid #eee;' />
                        <p>Gracias por confiar en nosotros.</p>
                    </div>";
                
                await _emailSender.SendEmailAsync(orderHEaderFromDb.ApplicationUser.Email, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
                // Log but don't break flow
            }

            TempData["success"] = "Pedido marcado como enviado.";
            return RedirectToAction(nameof(Details), new { orderId = orderHEaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDelivered()
        {
            var orderHEaderFromDb = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == OrderDetailsVM.OrderHeader.Id);
            if (orderHEaderFromDb == null) return NotFound();

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
            if (orderHEaderFromDb == null) return NotFound();

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

            // ── CRITICAL FIX: STOCK REVERSAL ──
            // Al cancelar, devolvemos los productos al inventario
            var orderDetails = await _unitOfWork.OrderDetail.GetAllAsync(u => u.OrderHeaderId == orderHEaderFromDb.Id, includeProperties: "Product");
            foreach (var detail in orderDetails)
            {
                if (detail.Product != null)
                {
                    detail.Product.Stock += detail.Count;
                    _unitOfWork.Product.Update(detail.Product);
                }
            }
            await _unitOfWork.SaveAsync();

            // EMAIL NOTIFICATION: Pedido Cancelado
            try 
            {
                string emailSubject = $"Pedido #{orderHEaderFromDb.Id} Cancelado - ElectronicJova";
                string emailBody = $@"
                    <div style='font-family: Arial, sans-serif; color: #333;'>
                        <h2 style='color: #dc3545;'>Pedido Cancelado</h2>
                        <p>Hola <strong>{orderHEaderFromDb.Name}</strong>,</p>
                        <p>Tu pedido <strong>#{orderHEaderFromDb.Id}</strong> ha sido cancelado.</p>
                        <p>Si ya habías realizado el pago, el reembolso ha sido procesado automáticamente.</p>
                        <hr style='border: 1px solid #eee;' />
                        <p>Lamentamos los inconvenientes.</p>
                    </div>";
                
                await _emailSender.SendEmailAsync(orderHEaderFromDb.ApplicationUser.Email, emailSubject, emailBody);
            }
            catch (Exception) { /* Ignore email errors */ }

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
