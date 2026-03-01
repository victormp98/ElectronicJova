using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Microsoft.AspNetCore.SignalR;
using ElectronicJova.Hubs;

namespace ElectronicJova.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhooksController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly StripeSettings _stripeSettings;
        private readonly IHubContext<OrderStatusHub> _hubContext;

        public WebhooksController(IUnitOfWork unitOfWork, IEmailSender emailSender, 
            IOptions<StripeSettings> stripeSettings, IHubContext<OrderStatusHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _stripeSettings = stripeSettings.Value;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

                try
                {
                    var stripeEvent = EventUtility.ConstructEvent(
                        json,
                        Request.Headers["Stripe-Signature"],
                        _stripeSettings.WebhookSecret
                    );

                    if (stripeEvent.Type == "checkout.session.completed")
                    {
                        var session = stripeEvent.Data.Object as Session;

                        // Fulfill the purchase
                        if (session != null && session.PaymentStatus == "paid")
                        {
                            var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.SessionId == session.Id, includeProperties: "ApplicationUser");

                            if (orderHeader != null && orderHeader.PaymentStatus != SD.PaymentStatusApproved)
                            {
                                var previousStatus = orderHeader.OrderStatus;
                                orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                                orderHeader.PaymentStatusValue = (int)SD.PaymentStatus.Approved;
                                orderHeader.OrderStatus = SD.StatusApproved;
                                orderHeader.OrderStatusValue = (int)SD.OrderStatus.Approved;
                                orderHeader.PaymentIntentId = session.PaymentIntentId;
                                await OrderStatusLogger.LogAsync(
                                    _unitOfWork,
                                    orderHeader.Id,
                                    previousStatus,
                                    orderHeader.OrderStatus,
                                    "system:webhook",
                                    "Pago confirmado por Stripe");
                                _unitOfWork.OrderHeader.Update(orderHeader);
                                await _unitOfWork.SaveAsync();

                                // Decrement Stock with safety check
                                var orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderHeader.Id, includeProperties: "Product");
                                foreach (var detail in orderDetails)
                                {
                                    var product = detail.Product;
                                    if (product != null)
                                    {
                                        if (product.Stock < detail.Count)
                                        {
                                            Console.WriteLine($"[CRITICAL] Stock insuficiente para Producto ID {product.Id} '{product.Name}'. Requerido: {detail.Count}, Disponible: {product.Stock}. Se procesará de todos modos pero requiere revisión.");
                                        }
                                        product.Stock -= detail.Count;
                                        _unitOfWork.Product.Update(product);
                                    }
                                }
                                await _unitOfWork.SaveAsync();

                                // SignalR: Notificar al cliente en tiempo real que el pago fue aprobado
                                await _hubContext.Clients.Group($"order-{orderHeader.Id}")
                                    .SendAsync("OrderStatusUpdated", SD.StatusApproved,
                                        SD.GetOrderStatusLabel(SD.StatusApproved),
                                        SD.GetOrderStatusIcon(SD.StatusApproved));

                                // EMAIL NOTIFICATION: Confirmación de Pedido
                                try 
                                {
                                    string emailSubject = $"Confirmación de Pedido #{orderHeader.Id} - ElectronicJova";
                                    string emailBody = $@"
                                        <div style='font-family: Arial, sans-serif; color: #333;'>
                                            <h2 style='color: #00d4ff;'>¡Gracias por tu compra!</h2>
                                            <p>Hola <strong>{orderHeader.Name}</strong>,</p>
                                            <p>Hemos recibido tu pago correctamente. Tu pedido <strong>#{orderHeader.Id}</strong> está siendo procesado.</p>
                                            <hr style='border: 1px solid #eee;' />
                                            <p><strong>Total:</strong> {orderHeader.OrderTotal:C}</p>
                                            <p>Puedes ver el estado de tu pedido en tiempo real entrando a tu cuenta.</p>
                                            <br/>
                                            <p style='font-size: 12px; color: #999;'>ElectronicJova Inc.</p>
                                        </div>";
                                    
                                    var targetEmail = session.CustomerDetails?.Email ?? orderHeader.ApplicationUser?.Email ?? session.CustomerEmail;
                                    if (!string.IsNullOrEmpty(targetEmail))
                                    {
                                        await _emailSender.SendEmailAsync(targetEmail, emailSubject, emailBody);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log error but don't fail the webhook
                                    Console.WriteLine($"Error sending email: {ex.Message}");
                                }
                            }
                        }
                    }

                    return Ok();
                }
                catch (StripeException)
                {
                    return BadRequest();
                }
        }
    }
}
