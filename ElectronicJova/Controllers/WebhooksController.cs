using System.Data;
using ElectronicJova.Data;
using ElectronicJova.Data.Repository;
using ElectronicJova.Hubs;
using ElectronicJova.Models;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace ElectronicJova.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhooksController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly StripeSettings _stripeSettings;
        private readonly IHubContext<OrderStatusHub> _hubContext;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            ApplicationDbContext db,
            IUnitOfWork unitOfWork,
            IEmailSender emailSender,
            IOptions<StripeSettings> stripeSettings,
            IHubContext<OrderStatusHub> hubContext,
            ILogger<WebhooksController> logger)
        {
            _db = db;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _stripeSettings = stripeSettings.Value;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var payload = await new StreamReader(Request.Body).ReadToEndAsync();
            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    payload,
                    Request.Headers["Stripe-Signature"],
                    _stripeSettings.WebhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Invalid Stripe webhook signature.");
                return BadRequest();
            }

            if (stripeEvent.Type != "checkout.session.completed" ||
                stripeEvent.Data.Object is not Session session ||
                session.PaymentStatus != "paid")
            {
                return Ok();
            }

            OrderHeader? confirmedOrder;

            // SERIALIZABLE serializa las lecturas y escrituras del pedido y del inventario.
            // Stripe reintenta respuestas 5xx; el estado aprobado evita volver a descontar stock.
            await using (var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                var order = _unitOfWork.OrderHeader.GetFirstOrDefault(
                    o => o.SessionId == session.Id,
                    includeProperties: "ApplicationUser");

                if (order is null)
                {
                    _logger.LogError("Paid Stripe session {SessionId} has no associated order.", session.Id);
                    return StatusCode(StatusCodes.Status503ServiceUnavailable);
                }

                if (order.PaymentStatus == SD.PaymentStatusApproved)
                {
                    return Ok();
                }

                var details = _unitOfWork.OrderDetail
                    .GetAll(d => d.OrderHeaderId == order.Id, includeProperties: "Product")
                    .ToList();

                if (details.Count == 0)
                {
                    _logger.LogError("Paid order {OrderId} has no order lines.", order.Id);
                    return StatusCode(StatusCodes.Status503ServiceUnavailable);
                }

                // Agrupar por producto impide descontar más unidades de las disponibles
                // cuando un pedido contiene varias líneas del mismo artículo.
                var stockRequirements = details
                    .GroupBy(d => d.ProductId)
                    .Select(group => new
                    {
                        Product = group.First().Product,
                        Quantity = group.Sum(detail => detail.Count)
                    })
                    .ToList();

                if (stockRequirements.Any(item =>
                    item.Product is null ||
                    item.Quantity <= 0 ||
                    item.Product.Stock < item.Quantity))
                {
                    _logger.LogError("Insufficient or invalid stock for paid order {OrderId}.", order.Id);
                    return StatusCode(StatusCodes.Status503ServiceUnavailable);
                }

                foreach (var requirement in stockRequirements)
                {
                    requirement.Product!.Stock -= requirement.Quantity;
                }

                var previousStatus = order.OrderStatus;
                order.PaymentStatus = SD.PaymentStatusApproved;
                order.PaymentStatusValue = (int)SD.PaymentStatus.Approved;
                order.OrderStatus = SD.StatusApproved;
                order.OrderStatusValue = (int)SD.OrderStatus.Approved;
                order.PaymentIntentId = session.PaymentIntentId;

                await OrderStatusLogger.LogAsync(
                    _unitOfWork,
                    order.Id,
                    previousStatus,
                    order.OrderStatus,
                    "system:webhook",
                    "Pago confirmado por Stripe");

                // Confirmación de pago, movimientos de stock y bitácora comparten el commit.
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();
                confirmedOrder = order;
            }

            try
            {
                await _hubContext.Clients.Group($"order-{confirmedOrder.Id}")
                    .SendAsync(
                        "OrderStatusUpdated",
                        SD.StatusApproved,
                        SD.GetOrderStatusLabel(SD.StatusApproved),
                        SD.GetOrderStatusIcon(SD.StatusApproved));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR notification failed for order {OrderId}.", confirmedOrder.Id);
            }

            try
            {
                var targetEmail = session.CustomerDetails?.Email
                    ?? confirmedOrder.ApplicationUser?.Email
                    ?? session.CustomerEmail;

                if (!string.IsNullOrWhiteSpace(targetEmail))
                {
                    var subject = $"Confirmación de Pedido #{confirmedOrder.Id} - ElectronicJova";
                    var body = $@"
                        <div style='font-family: Arial, sans-serif; color: #333;'>
                            <h2>Confirmación de pedido</h2>
                            <p>Hola <strong>{System.Net.WebUtility.HtmlEncode(confirmedOrder.Name)}</strong>,</p>
                            <p>El pago del pedido <strong>#{confirmedOrder.Id}</strong> fue confirmado.</p>
                            <p>Total: {confirmedOrder.OrderTotal:C}</p>
                        </div>";
                    await _emailSender.SendEmailAsync(targetEmail, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email notification failed for order {OrderId}.", confirmedOrder.Id);
            }

            return Ok();
        }
    }
}
