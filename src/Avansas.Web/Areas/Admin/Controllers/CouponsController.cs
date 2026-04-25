using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class CouponsController : Controller
{
    private readonly ICouponService _couponService;
    public CouponsController(ICouponService couponService) => _couponService = couponService;

    public async Task<IActionResult> Index()
    {
        var coupons = await _couponService.GetAllCouponsAsync();
        return View(coupons);
    }

    public IActionResult Create() => View(new CreateCouponDto());

    [HttpPost]
    public async Task<IActionResult> Create(CreateCouponDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _couponService.CreateCouponAsync(dto);
        TempData["Success"] = "Kupon oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _couponService.GetCouponByIdAsync(id);
        if (c == null) return NotFound();
        return View(new UpdateCouponDto
        {
            Id = c.Id, Code = c.Code, Description = c.Description, DiscountType = c.DiscountType,
            DiscountValue = c.DiscountValue, MinOrderAmount = c.MinOrderAmount,
            MaxDiscountAmount = c.MaxDiscountAmount, UsageLimit = c.UsageLimit,
            ValidFrom = c.ValidFrom, ValidTo = c.ValidTo, IsActive = c.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateCouponDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _couponService.UpdateCouponAsync(dto);
        TempData["Success"] = "Kupon güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _couponService.DeleteCouponAsync(id);
        TempData["Success"] = "Kupon silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        await _couponService.ToggleActiveAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
