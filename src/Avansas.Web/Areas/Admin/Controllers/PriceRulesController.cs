using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class PriceRulesController : Controller
{
    private readonly IPriceRuleService _priceRuleService;
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;

    public PriceRulesController(
        IPriceRuleService priceRuleService,
        IProductService productService,
        ICategoryService categoryService,
        IBrandService brandService)
    {
        _priceRuleService = priceRuleService;
        _productService = productService;
        _categoryService = categoryService;
        _brandService = brandService;
    }

    public async Task<IActionResult> Index()
    {
        var rules = await _priceRuleService.GetAllRulesAsync();
        return View(rules);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelectListsAsync();
        return View(new CreatePriceRuleDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePriceRuleDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            return View(dto);
        }
        try
        {
            await _priceRuleService.CreateRuleAsync(dto);
            TempData["Success"] = "Fiyat kuralı başarıyla oluşturuldu";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateSelectListsAsync();
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var rule = await _priceRuleService.GetRuleByIdAsync(id);
        if (rule == null) return NotFound();

        await PopulateSelectListsAsync();
        return View(rule);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(PriceRuleDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            return View(dto);
        }
        try
        {
            await _priceRuleService.UpdateRuleAsync(dto);
            TempData["Success"] = "Fiyat kuralı başarıyla güncellendi";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateSelectListsAsync();
            return View(dto);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _priceRuleService.DeleteRuleAsync(id);
        TempData["Success"] = "Fiyat kuralı silindi";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        await _priceRuleService.ToggleActiveAsync(id);
        TempData["Success"] = "Kural durumu güncellendi";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectListsAsync()
    {
        var products = await _productService.GetProductsAsync(new ProductFilterDto { PageSize = 1000 });
        var categories = await _categoryService.GetAllCategoriesAsync();
        var brands = await _brandService.GetAllBrandsAsync();

        ViewBag.Products = new SelectList(products.Items, "Id", "Name");
        ViewBag.Categories = new SelectList(categories, "Id", "Name");
        ViewBag.Brands = new SelectList(brands, "Id", "Name");
    }
}
