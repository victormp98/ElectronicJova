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
        private readonly IHubContext<OrderStatusHub> _hubContext;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<OrderController> _logger; // Injected Logger

        [BindProperty]
        public OrderDetailsVM OrderDetailsVM { get; set; } = new();

        public OrderController(IUnitOfWork unitOfWork, IHubContext<OrderStatusHub> hubContext, IEmailSender emailSender, ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? pageNumber)
        {
            try 
            {
                _logger.LogInformation("Admin Order Index access. Page={Page}", pageNumber);
                IQueryable<OrderHeader> orderHeaderQuery = _unitOfWork.OrderHeader.GetQueryable(includeProperties: "ApplicationUser", tracked: false);

                int pageSize = 10;
                var paginatedOrders = await PaginatedList<OrderHeader>.CreateAsync(orderHeaderQuery, pageNumber ?? 1, pageSize);
                
                _logger.LogInformation("Admin Order Index loaded {Count} orders.", paginatedOrders.Count);
                return View(paginatedOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FATAL ERROR in Admin Order Index. Context help: Verify OrderHeader table and ApplicationUser properties.");
                // Retornar una lista vacía para evitar el 500 mientras se investiga
                return View(new PaginatedList<OrderHeader>(new List<OrderHeader>(), 0, 1, 10));
            }
        }

        public async Task<IActionResult> Details(int orderId)
        {
            OrderDetailsVM = new OrderDetailsVM()
            {
                OrderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = await _unitOfWork.OrderDetail.GetAllAsync(u => u.OrderHeaderId == orderId, includeProperties: "Product"),
                StatusLogs = (await _unitOfWork.OrderStatusLog.GetAllAsync(u => u.OrderHeaderId == orderId))
                    .OrderByDescending(l => l.ChangedAt)
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

            var previousStatus = orderHEaderFromDb.OrderStatus;
            orderHEaderFromDb.OrderStatus = SD.StatusInProcess;
            orderHEaderFromDb.OrderStatusValue = (int)SD.OrderStatus.Processing;
            await OrderStatusLogger.LogAsync(
                _unitOfWork,
                orderHEaderFromDb.Id,
                previousStatus,
                orderHEaderFromDb.OrderStatus,
                User.Identity?.Name,
                "Pedido en proceso");
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

            var previousStatus = orderHEaderFromDb.OrderStatus;
            orderHEaderFromDb.TrackingNumber = OrderDetailsVM.OrderHeader.TrackingNumber;
            orderHEaderFromDb.Carrier = OrderDetailsVM.OrderHeader.Carrier;
            orderHEaderFromDb.OrderStatus = SD.StatusShipped;
            orderHEaderFromDb.OrderStatusValue = (int)SD.OrderStatus.Shipped;
            orderHEaderFromDb.ShippingDate = DateTime.Now;
            await OrderStatusLogger.LogAsync(
                _unitOfWork,
                orderHEaderFromDb.Id,
                previousStatus,
                orderHEaderFromDb.OrderStatus,
                User.Identity?.Name,
                "Pedido enviado");
            _unitOfWork.OrderHeader.Update(orderHEaderFromDb);
            await _unitOfWork.SaveAsync();

            // SignalR: notificar al cliente en tiempo real
            await _hubContext.Clients.Group($"order-{orderHEaderFromDb.Id}")
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
                
                if (orderHEaderFromDb.ApplicationUser != null && !string.IsNullOrEmpty(orderHEaderFromDb.ApplicationUser.Email))
                {
                    await _emailSender.SendEmailAsync(orderHEaderFromDb.ApplicationUser.Email, emailSubject, emailBody);
                }
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

            var previousStatus = orderHEaderFromDb.OrderStatus;
            orderHEaderFromDb.OrderStatus = SD.StatusDelivered;
            orderHEaderFromDb.OrderStatusValue = (int)SD.OrderStatus.Delivered;
            await OrderStatusLogger.LogAsync(
                _unitOfWork,
                orderHEaderFromDb.Id,
                previousStatus,
                orderHEaderFromDb.OrderStatus,
                User.Identity?.Name,
                "Pedido entregado");
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

            var previousStatus = orderHEaderFromDb.OrderStatus;
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
            await OrderStatusLogger.LogAsync(
                _unitOfWork,
                orderHEaderFromDb.Id,
                previousStatus,
                orderHEaderFromDb.OrderStatus,
                User.Identity?.Name,
                "Pedido cancelado por el admin");
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
                
                if (orderHEaderFromDb.ApplicationUser != null && !string.IsNullOrEmpty(orderHEaderFromDb.ApplicationUser.Email))
                {
                    await _emailSender.SendEmailAsync(orderHEaderFromDb.ApplicationUser.Email, emailSubject, emailBody);
                }
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
