using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Utilities;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace ElectronicJova.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhooksController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly StripeSettings _stripeSettings;

        public WebhooksController(IUnitOfWork unitOfWork, IEmailSender emailSender, IOptions<StripeSettings> stripeSettings)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _stripeSettings = stripeSettings.Value;
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
                        if (session.PaymentStatus == "paid")
                        {
                            var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.SessionId == session.Id);

                            if (orderHeader != null && orderHeader.PaymentStatus != SD.PaymentStatusApproved)
                            {
                                orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                                orderHeader.OrderStatus = SD.StatusApproved;
                                orderHeader.PaymentIntentId = session.PaymentIntentId;
                                _unitOfWork.OrderHeader.Update(orderHeader);
                                _unitOfWork.Save();

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
                                _unitOfWork.Save();
                            }
                        }
                    }

                    return Ok();
                }
                catch (StripeException e)
                {
                    return BadRequest();
                }
        }
    }
}
