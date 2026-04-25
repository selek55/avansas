using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class WarehousesController : Controller
{
    private readonly IWarehouseService _warehouseService;
    private readonly IProductService _productService;

    public WarehousesController(IWarehouseService warehouseService, IProductService productService)
    {
        _warehouseService = warehouseService;
        _productService = productService;
    }

    public async Task<IActionResult> Index()
    {
        var warehouses = await _warehouseService.GetAllWarehousesAsync();
        return View(warehouses);
    }

    public IActionResult Create()
    {
        return View(new CreateWarehouseDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWarehouseDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await _warehouseService.CreateWarehouseAsync(dto);
            TempData["Success"] = "Depo başarıyla oluşturuldu";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
        if (warehouse == null) return NotFound();
        return View(warehouse);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(WarehouseDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await _warehouseService.UpdateWarehouseAsync(dto);
            TempData["Success"] = "Depo başarıyla güncellendi";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _warehouseService.DeleteWarehouseAsync(id);
        TempData["Success"] = "Depo silindi";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Stocks(int id)
    {
        var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
        if (warehouse == null) return NotFound();

        var stocks = await _warehouseService.GetWarehouseStocksAsync(id);
        var products = await _productService.GetProductsAsync(new ProductFilterDto { PageSize = 1000 });

        ViewBag.Warehouse = warehouse;
        ViewBag.Products = new SelectList(products.Items, "Id", "Name");
        return View(stocks);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStock(int warehouseId, int productId, int quantity)
    {
        await _warehouseService.UpdateStockAsync(warehouseId, productId, quantity);
        TempData["Success"] = "Stok güncellendi";
        return RedirectToAction(nameof(Stocks), new { id = warehouseId });
    }
}
