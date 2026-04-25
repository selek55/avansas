using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Avansas.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IUserService _userService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IPaymentService _paymentService;

    public CheckoutController(ICartService cartService, IOrderService orderService, IUserService userService,
        ILoyaltyService loyaltyService, IPaymentService paymentService)
    {
        _cartService = cartService;
        _orderService = orderService;
        _userService = userService;
        _loyaltyService = loyaltyService;
        _paymentService = paymentService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync(UserId, null);
        if (cart == null || !cart.Items.Any()) return RedirectToAction("Index", "Cart");

        var addresses = await _userService.GetUserAddressesAsync(UserId);
        var shippingCost = await _orderService.CalculateShippingCostAsync(cart.SubTotal);

        ViewBag.Cart = cart;
        ViewBag.Addresses = addresses;
        ViewBag.ShippingCost = shippingCost;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        int shippingAddressId, int billingAddressId,
        PaymentMethod paymentMethod, string? note, string? couponCode,
        string? cardHolder, string? cardNumber, string? expireMonth,
        string? expireYear, string? cvc, int installment = 1)
    {
        try
        {
            var dto = new CreateOrderDto
            {
                UserId = UserId,
                ShippingAddressId = shippingAddressId,
                BillingAddressId = billingAddressId,
                PaymentMethod = paymentMethod,
                Note = note,
                CouponCode = couponCode
            };
            var orderId = await _orderService.CreateOrderFromCartAsync(dto);
            var order = await _orderService.GetOrderByIdAsync(orderId);

            // Kredi kartı ödemesi → iyzico 3DS akışı
            if (paymentMethod == PaymentMethod.CreditCard)
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var payDto = new InitiatePaymentDto(
                    orderId, UserId, ip,
                    cardHolder ?? "", cardNumber ?? "",
                    expireMonth ?? "", expireYear ?? "", cvc ?? "",
                    installment);

                var result = await _paymentService.InitiatePaymentAsync(payDto);

                if (!result.Success)
                {
                    TempData["Error"] = result.ErrorMessage;
                    return RedirectToAction(nameof(PaymentFailed), new { orderId });
                }

                // iyzico 3DS HTML form'unu göster
                ViewBag.HtmlContent = result.HtmlContent;
                return View("ThreeDSecure");
            }

            // Havale / Kapıda ödeme
            if (paymentMethod == PaymentMethod.BankTransfer)
            {
                ViewBag.Order = order;
                return View("BankTransferInfo");
            }

            await _loyaltyService.EarnOrderPointsAsync(UserId, order!.Total, orderId);
            return RedirectToAction(nameof(Confirmation), new { orderNumber = order!.OrderNumber });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>iyzico 3D Secure callback — banka POST yapar</summary>
    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ThreeDSecureCallback(string conversationId, string token)
    {
        var result = await _paymentService.Handle3DSecureCallbackAsync(conversationId, token);

        if (!result.Success)
        {
            TempData["Error"] = $"Ödeme başarısız: {result.ErrorMessage}";
            return RedirectToAction(nameof(PaymentFailed), new { orderId = result.OrderId });
        }

        var order = await _orderService.GetOrderByIdAsync(result.OrderId);
        if (order != null)
            await _loyaltyService.EarnOrderPointsAsync(order.UserId, order.Total, result.OrderId);

        return RedirectToAction(nameof(Confirmation), new { orderNumber = order?.OrderNumber });
    }

    /// <summary>BIN numarasına göre taksit seçeneklerini döner (AJAX)</summary>
    [HttpPost]
    public async Task<IActionResult> CheckInstallment(string binNumber, decimal price)
    {
        var result = await _paymentService.GetInstallmentInfoAsync(binNumber, price);
        return Json(result);
    }

    public IActionResult PaymentFailed(int orderId)
    {
        ViewBag.OrderId = orderId;
        return View();
    }

    public async Task<IActionResult> Confirmation(string orderNumber)
    {
        var order = await _orderService.GetOrderByNumberAsync(orderNumber);
        if (order == null || order.UserId != UserId) return NotFound();
        return View(order);
    }
}
