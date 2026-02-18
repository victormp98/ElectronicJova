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
                            var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.SessionId == session.Id);

                            if (orderHeader != null && orderHeader.PaymentStatus != SD.PaymentStatusApproved)
                            {
                                orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                                orderHeader.PaymentStatusValue = (int)SD.PaymentStatus.Approved;
                                orderHeader.OrderStatus = SD.StatusApproved;
                                orderHeader.OrderStatusValue = (int)SD.OrderStatus.Approved;
                                orderHeader.PaymentIntentId = session.PaymentIntentId;
                                _unitOfWork.OrderHeader.Update(orderHeader);
                                await _unitOfWork.SaveAsync();

                                // Decrement Stock
                                var orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderHeader.Id, includeProperties: "Product");
                                foreach (var detail in orderDetails)
                                {
                                    var product = detail.Product;
                                    if (product != null)
                                    {
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
