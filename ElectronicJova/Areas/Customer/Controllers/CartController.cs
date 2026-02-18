using ElectronicJova.Data.Repository;
using ElectronicJova.Models;
using ElectronicJova.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using ElectronicJova.Utilities;
using Stripe.Checkout;
using Stripe;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ElectronicJova.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        public CartVM CartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        // ─── Helper: obtiene el userId de forma segura ───────────────────────
        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // ─── Index ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            CartVM = new()
            {
                ShoppingCartList = await _unitOfWork.ShoppingCart.GetAllAsync(
                    u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach (var cart in CartVM.ShoppingCartList)
            {
                cart.Price = PricingCalculator.GetPriceBasedOnQuantity(cart);
                CartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(CartVM);
        }

        // ─── Summary GET ──────────────────────────────────────────────────────
        public async Task<IActionResult> Summary()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            CartVM = new()
            {
                ShoppingCartList = await _unitOfWork.ShoppingCart.GetAllAsync(
                    u => u.ApplicationUserId == userId, includeProperties: "Product")
            };

            CartVM.OrderHeader.ApplicationUser = await _unitOfWork.ApplicationUser
                .GetFirstOrDefaultAsync(u => u.Id == userId);

            if (CartVM.OrderHeader.ApplicationUser == null) return Unauthorized();

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

        // ─── Summary POST ─────────────────────────────────────────────────────
        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummaryPOST(CartVM cartVM)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            cartVM.ShoppingCartList = await _unitOfWork.ShoppingCart.GetAllAsync(
                u => u.ApplicationUserId == userId, includeProperties: "Product");

            // ── FIX 4: Re-validar stock antes de crear la sesión de Stripe ──
            foreach (var cart in cartVM.ShoppingCartList)
            {
                var product = await _unitOfWork.Product.GetFirstOrDefaultAsync(p => p.Id == cart.ProductId);
                if (product == null || product.Stock < cart.Count)
                {
                    TempData["error"] = $"'{product?.Title ?? "Un producto"}' ya no tiene suficiente stock disponible.";
                    return RedirectToAction(nameof(Index));
                }
            }

            cartVM.OrderHeader.OrderDate = System.DateTime.Now;
            cartVM.OrderHeader.ApplicationUserId = userId;
            cartVM.OrderHeader.OrderStatus = SD.StatusPending;
            cartVM.OrderHeader.OrderStatusValue = (int)SD.OrderStatus.Pending;
            cartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            cartVM.OrderHeader.PaymentStatusValue = (int)SD.PaymentStatus.Pending;

            // Obtener datos del usuario de forma segura
            var appUser = await _unitOfWork.ApplicationUser.GetFirstOrDefaultAsync(u => u.Id == userId);
            if (appUser != null)
            {
                cartVM.OrderHeader.PhoneNumber = appUser.PhoneNumber;
                cartVM.OrderHeader.Email = appUser.Email;
            }

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
                    Count = cart.Count,
                    SelectedOptions = cart.SelectedOptions,
                    SpecialNotes = cart.SpecialNotes
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
            }
            await _unitOfWork.SaveAsync();

            // ── Stripe Payment Session ──
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={cartVM.OrderHeader.Id}",
                CancelUrl = domain + $"customer/cart/PaymentCancelled?id={cartVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                CustomerEmail = appUser?.Email,
                ClientReferenceId = cartVM.OrderHeader.Id.ToString()
            };

            foreach (var item in cartVM.ShoppingCartList)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
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

            cartVM.OrderHeader.SessionId = session.Id;
            _unitOfWork.OrderHeader.Update(cartVM.OrderHeader);
            await _unitOfWork.SaveAsync();

            Response.Headers.Append("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // ─── Order Confirmation ───────────────────────────────────────────────
        // FIX 1: Ya NO confirma el pago aquí. El webhook es la única fuente de verdad.
        // FIX 5: Verifica que la orden pertenece al usuario actual.
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == id);

            if (orderHeader == null) return NotFound();

            // FIX 5: Ownership check — solo el dueño puede ver su confirmación
            if (orderHeader.ApplicationUserId != userId)
            {
                return Forbid();
            }

            // Solo mostramos la página. El webhook ya habrá procesado el pago,
            // decrementado el stock y enviado el email de confirmación.
            return View(orderHeader);
        }

        // ─── Payment Cancelled ────────────────────────────────────────────────
        // FIX 9: Marca la orden como Cancelled para no dejar registros basura.
        public async Task<IActionResult> PaymentCancelled(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var orderHeader = await _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == id);

            if (orderHeader != null && orderHeader.ApplicationUserId == userId
                && orderHeader.OrderStatus == SD.StatusPending)
            {
                orderHeader.OrderStatus = SD.StatusCancelled;
                orderHeader.OrderStatusValue = (int)SD.OrderStatus.Cancelled;
                orderHeader.PaymentStatus = SD.PaymentStatusPending; // pago no completado
                orderHeader.PaymentStatusValue = (int)SD.PaymentStatus.Pending;
                _unitOfWork.OrderHeader.Update(orderHeader);
                await _unitOfWork.SaveAsync();
            }

            return View(id);
        }

        // ─── Plus ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Plus(int cartId)
        {
            var cartFromDb = await _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(
                u => u.Id == cartId, includeProperties: "Product");

            if (cartFromDb == null) return NotFound();

            if (cartFromDb.Count < cartFromDb.Product.Stock)
            {
                cartFromDb.Count += 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                await _unitOfWork.SaveAsync();
            }
            else
            {
                TempData["error"] = "No puedes agregar más unidades. Stock máximo alcanzado.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─── Minus ────────────────────────────────────────────────────────────
        public async Task<IActionResult> Minus(int cartId)
        {
            var cartFromDb = await _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(u => u.Id == cartId);
            if (cartFromDb == null) return NotFound();

            if (cartFromDb.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(cartFromDb);
                await _unitOfWork.SaveAsync(); // FIX 8: guardar ANTES de contar

                var count = (await _unitOfWork.ShoppingCart.GetAllAsync(
                    u => u.ApplicationUserId == cartFromDb.ApplicationUserId)).Count();
                HttpContext.Session.SetInt32("CartItemCount", count);
            }
            else
            {
                cartFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                await _unitOfWork.SaveAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ─── Remove ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Remove(int cartId)
        {
            var cartFromDb = await _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(u => u.Id == cartId);
            if (cartFromDb == null) return NotFound();

            _unitOfWork.ShoppingCart.Remove(cartFromDb);
            await _unitOfWork.SaveAsync(); // FIX 8: guardar ANTES de contar

            var count = (await _unitOfWork.ShoppingCart.GetAllAsync(
                u => u.ApplicationUserId == cartFromDb.ApplicationUserId)).Count();
            HttpContext.Session.SetInt32("CartItemCount", count);

            return RedirectToAction(nameof(Index));
        }
    }
}
