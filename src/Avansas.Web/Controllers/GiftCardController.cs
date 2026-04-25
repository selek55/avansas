using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Avansas.Web.Controllers;

public class GiftCardController : Controller
{
    private readonly IGiftCardService _giftCardService;

    public GiftCardController(IGiftCardService giftCardService)
    {
        _giftCardService = giftCardService;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public IActionResult Index() => View();

    [Authorize]
    public async Task<IActionResult> MyCards()
    {
        var cards = await _giftCardService.GetUserGiftCardsAsync(UserId!);
        return View(cards);
    }

    [HttpGet]
    public IActionResult Purchase() => View();

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Purchase(decimal amount, string recipientEmail, string? recipientName, string? personalMessage)
    {
        if (amount < 50 || amount > 5000)
        {
            ModelState.AddModelError("", "Hediye kartı tutarı 50 ₺ ile 5.000 ₺ arasında olmalıdır.");
            return View();
        }

        var dto = new PurchaseGiftCardDto(UserId!, amount, recipientEmail, recipientName, personalMessage);
        var card = await _giftCardService.PurchaseGiftCardAsync(dto);

        TempData["Success"] = $"Hediye kartı oluşturuldu! Kod: {card.Code}";
        return RedirectToAction(nameof(MyCards));
    }

    [HttpGet]
    public async Task<IActionResult> CheckBalance(string code)
    {
        var card = await _giftCardService.GetByCodeAsync(code);
        return Json(card != null
            ? new { success = true, balance = card.RemainingBalance, expiresAt = card.ExpiresAt.ToString("dd.MM.yyyy") }
            : new { success = false, balance = 0m, expiresAt = "" });
    }
}
