using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService) => _reviewService = reviewService;

    public async Task<IActionResult> Index(int page = 1, bool? isApproved = null)
    {
        var reviews = await _reviewService.GetAllReviewsAsync(page, 20, isApproved);
        ViewBag.IsApprovedFilter = isApproved;
        return View(reviews);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        await _reviewService.ApproveReviewAsync(id);
        TempData["Success"] = "Yorum onaylandı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _reviewService.DeleteReviewAsync(id);
        TempData["Success"] = "Yorum silindi.";
        return RedirectToAction(nameof(Index));
    }
}
