using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;
    private readonly IFileService _fileService;

    public ProductsController(IProductService productService, ICategoryService categoryService, IBrandService brandService, IFileService fileService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _brandService = brandService;
        _fileService = fileService;
    }

    public async Task<IActionResult> Index([FromQuery] ProductFilterDto filter)
    {
        var result = await _productService.GetProductsAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View(new CreateProductDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto, IFormFile? mainImageFile)
    {
        if (!ModelState.IsValid) { await PopulateCategoriesAsync(); return View(dto); }
        try
        {
            if (mainImageFile != null && mainImageFile.Length > 0)
            {
                if (!_fileService.IsValidImageFile(mainImageFile.FileName, mainImageFile.Length))
                    ModelState.AddModelError("mainImageFile", "Geçersiz dosya. Max 10MB, desteklenen: jpg, jpeg, png, webp, gif");

                if (!ModelState.IsValid) { await PopulateCategoriesAsync(); return View(dto); }

                await using var stream = mainImageFile.OpenReadStream();
                dto.MainImageUrl = await _fileService.SaveFileAsync(stream, mainImageFile.FileName, "images/products");
            }

            var id = await _productService.CreateProductAsync(dto);

            if (mainImageFile != null && !string.IsNullOrEmpty(dto.MainImageUrl))
                await _productService.AddProductImageAsync(id, dto.MainImageUrl, isMain: true);

            TempData["Success"] = "Ürün başarıyla oluşturuldu. Ek görseller ekleyebilirsiniz.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCategoriesAsync();
            return View(dto);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();

        await PopulateCategoriesAsync();
        var dto = new UpdateProductDto
        {
            Id = product.Id, Name = product.Name, Slug = product.Slug, ShortDescription = product.ShortDescription,
            Description = product.Description, SKU = product.SKU, Price = product.Price,
            DiscountedPrice = product.DiscountedPrice, StockQuantity = product.StockQuantity,
            IsActive = product.IsActive, IsFeatured = product.IsFeatured, IsNewProduct = product.IsNewProduct,
            CategoryId = product.CategoryId, BrandId = product.BrandId, TaxRate = product.TaxRate,
            MainImageUrl = product.MainImageUrl
        };
        ViewBag.ProductImages = product.Images;
        ViewBag.Variants = await _productService.GetProductVariantsAsync(id);
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateProductDto dto, IFormFile? mainImageFile)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync();
            ViewBag.ProductImages = await _productService.GetProductImagesAsync(dto.Id);
            ViewBag.Variants = await _productService.GetProductVariantsAsync(dto.Id);
            return View(dto);
        }
        try
        {
            if (mainImageFile != null && mainImageFile.Length > 0)
            {
                if (!_fileService.IsValidImageFile(mainImageFile.FileName, mainImageFile.Length))
                {
                    ModelState.AddModelError("mainImageFile", "Geçersiz dosya. Max 10MB, desteklenen: jpg, jpeg, png, webp, gif");
                    await PopulateCategoriesAsync();
                    ViewBag.ProductImages = await _productService.GetProductImagesAsync(dto.Id);
                    ViewBag.Variants = await _productService.GetProductVariantsAsync(dto.Id);
                    return View(dto);
                }

                if (!string.IsNullOrEmpty(dto.MainImageUrl))
                    await _fileService.DeleteFileAsync(dto.MainImageUrl);

                await using var stream = mainImageFile.OpenReadStream();
                dto.MainImageUrl = await _fileService.SaveFileAsync(stream, mainImageFile.FileName, "images/products");
                await _productService.AddProductImageAsync(dto.Id, dto.MainImageUrl, isMain: true);
            }

            await _productService.UpdateProductAsync(dto);
            TempData["Success"] = "Ürün başarıyla güncellendi";
            return RedirectToAction(nameof(Edit), new { id = dto.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCategoriesAsync();
            ViewBag.ProductImages = await _productService.GetProductImagesAsync(dto.Id);
            ViewBag.Variants = await _productService.GetProductVariantsAsync(dto.Id);
            return View(dto);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteProductAsync(id);
        TempData["Success"] = "Ürün silindi";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStock(int productId, int quantity)
    {
        await _productService.UpdateStockAsync(productId, quantity);
        return Json(new { success = true });
    }

    // ── Variant Endpoints ──────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddVariant(int productId, [FromForm] ProductVariantDto dto)
    {
        try
        {
            dto.ProductId = productId;
            var id = await _productService.AddVariantAsync(productId, dto);
            TempData["Success"] = "Varyant eklendi";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateVariant([FromForm] ProductVariantDto dto)
    {
        try
        {
            await _productService.UpdateVariantAsync(dto);
            TempData["Success"] = "Varyant güncellendi";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Edit), new { id = dto.ProductId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteVariant(int id, int productId)
    {
        try
        {
            await _productService.DeleteVariantAsync(id);
            TempData["Success"] = "Varyant silindi";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    // ── Bulk Import / Export ─────────────────────────────────

    public async Task<IActionResult> ExportProducts()
    {
        var filter = new ProductFilterDto { PageSize = 10000 };
        var result = await _productService.GetProductsAsync(filter);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Urunler");

        // Headers
        var headers = new[] { "Ad", "SKU", "Fiyat", "Indirimli Fiyat", "Stok", "Kategori", "Marka", "Aktif" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;

        // Data
        int row = 2;
        foreach (var p in result.Items)
        {
            ws.Cell(row, 1).Value = p.Name;
            ws.Cell(row, 2).Value = p.Slug; // SKU is not in ProductListDto, use Slug as identifier
            ws.Cell(row, 3).Value = p.Price;
            ws.Cell(row, 4).Value = p.DiscountedPrice ?? 0;
            ws.Cell(row, 5).Value = p.StockQuantity;
            ws.Cell(row, 6).Value = p.CategoryName ?? "";
            ws.Cell(row, 7).Value = p.BrandName ?? "";
            ws.Cell(row, 8).Value = p.IsActive ? "Evet" : "Hayir";
            row++;
        }
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"urunler_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    public IActionResult ImportProducts()
    {
        return View("Import");
    }

    [HttpPost]
    public async Task<IActionResult> ImportProducts(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Lutfen bir Excel dosyasi secin.";
            return View("Import");
        }

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var rows = ws.RowsUsed().Skip(1); // skip header

            var categories = await _categoryService.GetAllCategoriesAsync();
            var brands = await _brandService.GetAllBrandsAsync();

            int created = 0, updated = 0, errors = 0;

            foreach (var row in rows)
            {
                try
                {
                    var name = row.Cell(1).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var sku = row.Cell(2).GetString().Trim();
                    var price = row.Cell(3).GetValue<decimal>();
                    var discountedPrice = row.Cell(4).IsEmpty() ? (decimal?)null : row.Cell(4).GetValue<decimal>();
                    if (discountedPrice == 0) discountedPrice = null;
                    var stock = row.Cell(5).IsEmpty() ? 0 : row.Cell(5).GetValue<int>();
                    var categoryName = row.Cell(6).GetString().Trim();
                    var brandName = row.Cell(7).GetString().Trim();
                    var isActive = row.Cell(8).GetString().Trim().ToLowerInvariant() != "hayir";

                    var category = categories.FirstOrDefault(c =>
                        c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    var brand = brands.FirstOrDefault(b =>
                        b.Name.Equals(brandName, StringComparison.OrdinalIgnoreCase));

                    // Try to find existing product by SKU/slug
                    ProductDto? existing = null;
                    if (!string.IsNullOrEmpty(sku))
                        existing = await _productService.GetProductBySlugAsync(sku);

                    if (existing != null)
                    {
                        var updateDto = new UpdateProductDto
                        {
                            Id = existing.Id, Name = name, SKU = existing.SKU,
                            Slug = existing.Slug, Price = price,
                            DiscountedPrice = discountedPrice, StockQuantity = stock,
                            CategoryId = category?.Id ?? existing.CategoryId,
                            BrandId = brand?.Id ?? existing.BrandId,
                            IsActive = isActive, TaxRate = existing.TaxRate
                        };
                        await _productService.UpdateProductAsync(updateDto);
                        updated++;
                    }
                    else
                    {
                        var createDto = new CreateProductDto
                        {
                            Name = name, SKU = sku.Length > 0 ? sku : name[..Math.Min(name.Length, 10)],
                            Price = price, DiscountedPrice = discountedPrice, StockQuantity = stock,
                            CategoryId = category?.Id ?? 1, BrandId = brand?.Id,
                            IsActive = isActive, TaxRate = 18
                        };
                        await _productService.CreateProductAsync(createDto);
                        created++;
                    }
                }
                catch
                {
                    errors++;
                }
            }

            TempData["Success"] = $"Icerik aktarimi tamamlandi. {created} yeni urun eklendi, {updated} urun guncellendi" +
                                  (errors > 0 ? $", {errors} satir hatali." : ".");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Dosya isleme hatasi: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sablon");
        var headers = new[] { "Ad", "SKU", "Fiyat", "Indirimli Fiyat", "Stok", "Kategori", "Marka", "Aktif" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;

        // Example row
        ws.Cell(2, 1).Value = "Ornek Urun";
        ws.Cell(2, 2).Value = "ornek-urun";
        ws.Cell(2, 3).Value = 99.90;
        ws.Cell(2, 4).Value = 79.90;
        ws.Cell(2, 5).Value = 100;
        ws.Cell(2, 6).Value = "Kirtasiye";
        ws.Cell(2, 7).Value = "Faber-Castell";
        ws.Cell(2, 8).Value = "Evet";
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "urun_sablonu.xlsx");
    }

    private async Task PopulateCategoriesAsync()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name");
    }
}
