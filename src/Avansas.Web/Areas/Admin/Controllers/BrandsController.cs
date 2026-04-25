using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class BrandsController : Controller
{
    private readonly IBrandService _brandService;
    public BrandsController(IBrandService brandService) => _brandService = brandService;

    public async Task<IActionResult> Index()
    {
        var brands = await _brandService.GetAllBrandsAsync();
        return View(brands);
    }

    public IActionResult Create() => View(new CreateBrandDto());

    [HttpPost]
    public async Task<IActionResult> Create(CreateBrandDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _brandService.CreateBrandAsync(dto);
        TempData["Success"] = "Marka oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var brand = await _brandService.GetBrandByIdAsync(id);
        if (brand == null) return NotFound();
        return View(new UpdateBrandDto { Id = brand.Id, Name = brand.Name, Slug = brand.Slug, LogoUrl = brand.LogoUrl, Description = brand.Description, IsActive = brand.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateBrandDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        await _brandService.UpdateBrandAsync(dto);
        TempData["Success"] = "Marka güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _brandService.DeleteBrandAsync(id);
        TempData["Success"] = "Marka silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        await _brandService.ToggleActiveAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
