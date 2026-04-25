using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Avansas.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService) => _cartService = cartService;

    private string? UserId => User.Identity?.IsAuthenticated == true
        ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;

    private string SessionId
    {
        get
        {
            // Session cookie'nin gönderilmesi için bir değer yazılması gerekir
            if (HttpContext.Session.GetString("_sid") == null)
                HttpContext.Session.SetString("_sid", HttpContext.Session.Id);
            return HttpContext.Session.Id;
        }
    }

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync(UserId, SessionId);
        return View(cart);
    }

    [HttpPost]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> Add([FromBody] AddToCartDto dto)
    {
        try
        {
            var cart = await _cartService.AddToCartAsync(UserId, SessionId, dto);
            return Json(new { success = true, itemCount = cart.ItemCount, message = "Ürün sepete eklendi" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> Update([FromBody] UpdateCartItemDto dto)
    {
        try
        {
            var cart = await _cartService.UpdateCartItemAsync(UserId, SessionId, dto);
            return Json(new { success = true, cart });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        try
        {
            var cart = await _cartService.RemoveFromCartAsync(UserId, SessionId, cartItemId);
            return Json(new { success = true, cart });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string couponCode)
    {
        try
        {
            var cart = await _cartService.ApplyCouponAsync(UserId, SessionId, couponCode);
            return Json(new { success = true, cart, message = "Kupon uygulandı" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCoupon()
    {
        await _cartService.RemoveCouponAsync(UserId, SessionId);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var count = await _cartService.GetCartItemCountAsync(UserId, SessionId);
        return Json(count);
    }
}
