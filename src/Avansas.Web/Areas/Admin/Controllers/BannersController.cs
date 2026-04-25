using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class BannersController : Controller
{
    private readonly IBannerService _bannerService;
    public BannersController(IBannerService bannerService) => _bannerService = bannerService;

    public async Task<IActionResult> Index()
    {
        var banners = await _bannerService.GetAllBannersAsync();
        return View(banners);
    }

    public IActionResult Create() => View(new CreateBannerDto());

    [HttpPost]
    public async Task<IActionResult> Create(CreateBannerDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _bannerService.CreateBannerAsync(dto);
        TempData["Success"] = "Banner oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var b = await _bannerService.GetBannerByIdAsync(id);
        if (b == null) return NotFound();
        return View(new UpdateBannerDto
        {
            Id = b.Id, Title = b.Title, SubTitle = b.SubTitle, ImageUrl = b.ImageUrl,
            MobileImageUrl = b.MobileImageUrl, LinkUrl = b.LinkUrl, ButtonText = b.ButtonText,
            DisplayOrder = b.DisplayOrder, IsActive = b.IsActive, ValidFrom = b.ValidFrom, ValidTo = b.ValidTo
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateBannerDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _bannerService.UpdateBannerAsync(dto);
        TempData["Success"] = "Banner güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _bannerService.DeleteBannerAsync(id);
        TempData["Success"] = "Banner silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        await _bannerService.ToggleActiveAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
