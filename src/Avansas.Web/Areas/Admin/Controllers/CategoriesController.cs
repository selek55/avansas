using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService) => _categoryService = categoryService;

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return View(categories);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View(new CreateCategoryDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        if (!ModelState.IsValid) { await PopulateCategoriesAsync(); return View(dto); }
        await _categoryService.CreateCategoryAsync(dto);
        TempData["Success"] = "Kategori oluşturuldu";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();
        await PopulateCategoriesAsync(id);
        var dto = new UpdateCategoryDto
        {
            Id = category.Id, Name = category.Name, Slug = category.Slug, Description = category.Description,
            ImageUrl = category.ImageUrl, ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder, IsActive = category.IsActive
        };
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid) { await PopulateCategoriesAsync(dto.Id); return View(dto); }
        await _categoryService.UpdateCategoryAsync(dto);
        TempData["Success"] = "Kategori güncellendi";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _categoryService.DeleteCategoryAsync(id);
        TempData["Success"] = "Kategori silindi";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync(int? excludeId = null)
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        if (excludeId.HasValue) categories = categories.Where(c => c.Id != excludeId.Value).ToList();
        ViewBag.ParentCategories = new SelectList(categories, "Id", "Name");
    }
}
