using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace Avansas.Web.Controllers;

[Route("karsilastir")]
public class CompareController : Controller
{
    private readonly IProductService _productService;
    private const string SessionKey = "CompareList";
    private const int MaxCompare = 4;

    public CompareController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var ids = GetCompareIds();
        var products = new List<Avansas.Application.DTOs.ProductDto>();

        foreach (var id in ids)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null) products.Add(product);
        }

        return View(products);
    }

    [HttpPost("ekle")]
    [EnableRateLimiting("api")]
    public IActionResult Add(int productId)
    {
        var ids = GetCompareIds();

        if (ids.Contains(productId))
            return Json(new { success = false, message = "Bu ürün zaten karşılaştırma listesinde.", count = ids.Count });

        if (ids.Count >= MaxCompare)
            return Json(new { success = false, message = $"En fazla {MaxCompare} ürün karşılaştırabilirsiniz.", count = ids.Count });

        ids.Add(productId);
        SaveCompareIds(ids);
        return Json(new { success = true, message = "Ürün karşılaştırma listesine eklendi.", count = ids.Count });
    }

    [HttpPost("sil")]
    public IActionResult Remove(int productId)
    {
        var ids = GetCompareIds();
        ids.Remove(productId);
        SaveCompareIds(ids);
        return Json(new { success = true, count = ids.Count });
    }

    [HttpGet("adet")]
    public IActionResult GetCount()
    {
        var ids = GetCompareIds();
        return Json(ids.Count);
    }

    private List<int> GetCompareIds()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return new List<int>();
        return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
    }

    private void SaveCompareIds(List<int> ids)
    {
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(ids));
    }
}
