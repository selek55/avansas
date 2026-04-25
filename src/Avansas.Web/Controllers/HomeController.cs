using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IBannerService _bannerService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IProductService productService, ICategoryService categoryService, IBannerService bannerService, ILogger<HomeController> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _bannerService = bannerService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var featuredProducts = await _productService.GetFeaturedProductsAsync(8);
        var newProducts = await _productService.GetNewProductsAsync(8);
        var categories = await _categoryService.GetRootCategoriesAsync();
        var banners = await _bannerService.GetActiveBannersAsync();

        ViewBag.FeaturedProducts = featuredProducts;
        ViewBag.NewProducts = newProducts;
        ViewBag.Categories = categories;
        ViewBag.Banners = banners;

        ViewData["MetaTitle"] = "Avansas - Ofis Malzemeleri ve Kırtasiye Online Alışveriş";
        ViewData["MetaDescription"] = "Türkiye'nin en büyük online ofis malzemeleri mağazası. Binlerce üründe uygun fiyat ve hızlı teslimat.";
        return View();
    }

    [Route("hata")]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature?.Error != null)
            _logger.LogError(exceptionFeature.Error, "İşlenmeyen hata: {Path}", exceptionFeature.Path);

        Response.StatusCode = 500;
        return View();
    }

    [Route("hata/{statusCode:int}")]
    public IActionResult StatusCodeError(int statusCode)
    {
        if (statusCode == 404)
        {
            var originalPath = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath ?? "";
            _logger.LogWarning("404 - Sayfa bulunamadı: {Path}", originalPath);
            return View("NotFound");
        }

        return View("Error");
    }
}
