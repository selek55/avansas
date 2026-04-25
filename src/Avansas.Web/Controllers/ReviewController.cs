using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Avansas.Web.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly ILoyaltyService _loyaltyService;

    public ReviewController(IReviewService reviewService, ILoyaltyService loyaltyService)
    {
        _reviewService = reviewService;
        _loyaltyService = loyaltyService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewDto dto)
    {
        dto.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (dto.Rating < 1 || dto.Rating > 5)
        {
            TempData["Error"] = "Lütfen 1-5 arası puan seçin.";
            return RedirectToAction("Detail", "Product", new { slug = TempData["ProductSlug"] });
        }

        try
        {
            await _reviewService.CreateReviewAsync(dto);
            TempData["Success"] = "Yorumunuz alındı, onay bekliyor.";

            // Yorum bonusu: 10 sadakat puanı
            await _loyaltyService.AddPointsAsync(dto.UserId, 10, "Ürün yorumu bonusu", LoyaltyType.ReviewBonus);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect(Request.Headers.Referer.ToString() ?? "/");
    }
}
