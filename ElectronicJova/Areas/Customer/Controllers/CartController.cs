using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Added for Session
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks; // Added for async/await
using ElectronicJova.Utilities; // Added for SD
using Stripe.Checkout; // Added for Stripe Checkout
using Stripe; // Added for StripeConfiguration and other Stripe types
using Microsoft.AspNetCore.Identity.UI.Services; // Added for IEmailSender

namespace ElectronicJova.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender; // Injected IEmailSender
        public CartVM CartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender) // Added IEmailSender to constructor
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            CartVM = new()
            {
                ShoppingCartList = await _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach (var cart in CartVM.ShoppingCartList)
            {
                cart.Price = PricingCalculator.GetPriceBasedOnQuantity(cart);
                CartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(CartVM);
        }

        public async Task<IActionResult> Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            CartVM = new()
            {
                ShoppingCartList = await _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == userId, includeProperties: "Product")
            };

            CartVM.OrderHeader.ApplicationUser = await _unitOfWork.ApplicationUser.GetFirstOrDefaultAsync(u => u.Id == userId);

            CartVM.OrderHeader.Name = CartVM.OrderHeader.ApplicationUser.Name;
            CartVM.OrderHeader.PhoneNumber = CartVM.OrderHeader.ApplicationUser.PhoneNumber;
            CartVM.OrderHeader.StreetAddress = CartVM.OrderHeader.ApplicationUser.StreetAddress;
            CartVM.OrderHeader.City = CartVM.OrderHeader.ApplicationUser.City;
            CartVM.OrderHeader.State = CartVM.OrderHeader.ApplicationUser.State;
            CartVM.OrderHeader.PostalCode = CartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in CartVM.ShoppingCartList)
            {
                cart.Price = PricingCalculator.GetPriceBasedOnQuantity(cart);
                CartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(CartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummaryPOST(CartVM cartVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            cartVM.ShoppingCartList = await _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == userId, includeProperties: "Product");

            cartVM.OrderHeader.OrderDate = System.DateTime.Now;
            cartVM.OrderHeader.ApplicationUserId = userId;
            cartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            cartVM.OrderHeader.OrderStatus = SD.StatusPending;
            // Copy PhoneNumber and Email from ApplicationUser to OrderHeader for historical data
            cartVM.OrderHeader.PhoneNumber = cartVM.OrderHeader.ApplicationUser.PhoneNumber;
            cartVM.OrderHeader.Email = cartVM.OrderHeader.ApplicationUser.Email;

            foreach (var cart in cartVM.ShoppingCartList)
            {
                cart.Price = PricingCalculator.GetPriceBasedOnQuantity(cart);
                cartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            _unitOfWork.OrderHeader.Add(cartVM.OrderHeader);
            await _unitOfWork.SaveAsync();

            foreach (var cart in cartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = cartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
            }
            await _unitOfWork.SaveAsync();

            // Stripe Payment Logic
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={cartVM.OrderHeader.Id}", // Updated to call a modified OrderConfirmation
                CancelUrl = domain + $"customer/cart/PaymentCancelled?id={cartVM.OrderHeader.Id}", // New PaymentCancelled action
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                CustomerEmail = cartVM.OrderHeader.ApplicationUser.Email, // Ensure user email is available in OrderHeader.ApplicationUser.Email
                ClientReferenceId = cartVM.OrderHeader.Id.ToString()
            };

            foreach (var item in cartVM.ShoppingCartList)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100), // convert to cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            _unitOfWork.OrderHeader.Update(cartVM.OrderHeader); // Update order with SessionId and PaymentIntentId
            cartVM.OrderHeader.SessionId = session.Id;
            cartVM.OrderHeader.PaymentIntentId = session.PaymentIntentId; // This might be null for Checkout sessions, but keep for consistency

            await _unitOfWork.SaveAsync();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public async Task<IActionResult> OrderConfirmation(int id)
        {
            OrderHeader orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == id);
            
            // Check if payment was successful through Stripe
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus == "paid")
            {
                _unitOfWork.OrderHeader.Update(orderHeader);
                orderHeader.PaymentStatus = SD.PaymentStatusApproved;
                orderHeader.OrderStatus = SD.StatusApproved;
                orderHeader.PaymentIntentId = session.PaymentIntentId; // Ensure this is set
                await _unitOfWork.SaveAsync();
                
                // Clear cart and session
                var shoppingCarts = await _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == orderHeader.ApplicationUserId);
                _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
                await _unitOfWork.SaveAsync();
                HttpContext.Session.SetInt32("CartItemCount", 0);

                // Send order confirmation email
                var userEmail = (await _unitOfWork.ApplicationUser.GetFirstOrDefaultAsync(u => u.Id == orderHeader.ApplicationUserId)).Email;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    await _emailSender.SendEmailAsync(
                        userEmail,
                        "Order Confirmation - ElectronicJova",
                        $"<p>Thank you for your order! Your Order ID is: <strong>{orderHeader.Id}</strong></p>" +
                        "<p>Your payment was successful. We've received your order and will process it shortly.</p>" +
                        "<p>You can view your order details by logging into your account.</p>"
                    );
                }
            }

            return View(orderHeader); // Pass the entire OrderHeader object to the view
        }

        public async Task<IActionResult> PaymentCancelled(int id)
        {
            OrderHeader orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == id);
            // Optionally, mark order as cancelled or pending review if needed
            // For now, just display a cancellation message
            return View(id);
        }


        public async Task<IActionResult> Plus(int cartId)
        {
            var cartFromDb = await _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(u => u.Id == cartId);
            cartFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            await _unitOfWork.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cartFromDb = await _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(u => u.Id == cartId);
            if (cartFromDb.Count <= 1)
            {
                // Remove from cart
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                HttpContext.Session.SetInt32("CartItemCount", 
                    (await _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == cartFromDb.ApplicationUserId)).Count() - 1);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            await _unitOfWork.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cartFromDb = await _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(u => u.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            HttpContext.Session.SetInt32("CartItemCount", 
                (await _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == cartFromDb.ApplicationUserId)).Count() - 1);
            await _unitOfWork.SaveAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}


