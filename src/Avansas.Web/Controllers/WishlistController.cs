using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Avansas.Web.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService) => _wishlistService = wishlistService;

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var items = await _wishlistService.GetWishlistAsync(userId);
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isIn = await _wishlistService.IsInWishlistAsync(userId, productId);

        if (isIn)
            await _wishlistService.RemoveFromWishlistAsync(userId, productId);
        else
            await _wishlistService.AddToWishlistAsync(userId, productId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { inWishlist = !isIn });

        return Redirect(Request.Headers.Referer.ToString() ?? "/");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _wishlistService.RemoveFromWishlistAsync(userId, productId);
        TempData["Success"] = "Ürün istek listesinden çıkarıldı.";
        return RedirectToAction(nameof(Index));
    }
}
