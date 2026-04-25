using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Avansas.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IReviewService _reviewService;
    private readonly IStockNotificationService _stockNotificationService;
    private readonly IProductQuestionService _productQuestionService;
    private readonly IBrandService _brandService;
    private readonly IRecommendationService _recommendationService;

    public ProductController(
        IProductService productService,
        ICategoryService categoryService,
        IReviewService reviewService,
        IStockNotificationService stockNotificationService,
        IProductQuestionService productQuestionService,
        IBrandService brandService,
        IRecommendationService recommendationService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _reviewService = reviewService;
        _stockNotificationService = stockNotificationService;
        _productQuestionService = productQuestionService;
        _brandService = brandService;
        _recommendationService = recommendationService;
    }

    public async Task<IActionResult> Index([FromQuery] ProductFilterDto filter)
    {
        var result = await _productService.GetProductsAsync(filter);
        var categories = await _categoryService.GetRootCategoriesAsync();
        var brands = await _brandService.GetAllBrandsAsync(true);
        ViewBag.Categories = categories;
        ViewBag.Brands = brands;
        ViewBag.Filter = filter;

        ViewData["MetaTitle"] = "Tüm Ürünler - Avansas";
        ViewData["MetaDescription"] = "Avansas'ta binlerce ofis malzemesi, kırtasiye ve teknoloji ürününü keşfedin. Uygun fiyat ve hızlı kargo.";
        return View(result);
    }

    public async Task<IActionResult> Detail(string slug)
    {
        var product = await _productService.GetProductBySlugAsync(slug);
        if (product == null) return NotFound();

        var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        var sessionId = HttpContext.Session.Id;

        // Görüntüleme kaydet + öneriler — fire-and-forget ile ana akışı engelleme
        _ = Task.Run(async () =>
        {
            await _recommendationService.RecordProductViewAsync(product.Id, userId, sessionId);
        });

        var relatedTask = _productService.GetRelatedProductsAsync(product.Id);
        var boughtTogetherTask = _recommendationService.GetFrequentlyBoughtTogetherAsync(product.Id, 6);
        var similarTask = _recommendationService.GetSimilarProductsAsync(product.Id, 6);
        var reviewsTask = _reviewService.GetApprovedReviewsAsync(product.Id);
        var avgRatingTask = _reviewService.GetAverageRatingAsync(product.Id);
        var questionsTask = _productQuestionService.GetApprovedQuestionsAsync(product.Id);
        var variantsTask = _productService.GetProductVariantsAsync(product.Id);

        await Task.WhenAll(relatedTask, boughtTogetherTask, similarTask, reviewsTask, avgRatingTask, questionsTask, variantsTask);

        var related = relatedTask.Result;
        var boughtTogether = boughtTogetherTask.Result;
        var similar = similarTask.Result;
        var reviews = reviewsTask.Result;
        var avgRating = avgRatingTask.Result;
        var questions = questionsTask.Result;
        var variants = variantsTask.Result;

        ViewBag.RelatedProducts = related;
        ViewBag.FrequentlyBoughtTogether = boughtTogether;
        ViewBag.SimilarProducts = similar;
        ViewBag.Reviews = reviews;
        ViewBag.AvgRating = avgRating;
        ViewBag.Questions = questions;
        ViewBag.Variants = variants;

        if (userId != null)
            ViewBag.HasReviewed = await _reviewService.HasUserReviewedAsync(product.Id, userId);

        return View(product);
    }

    public async Task<IActionResult> Category(string slug, [FromQuery] ProductFilterDto filter)
    {
        var category = await _categoryService.GetCategoryBySlugAsync(slug);
        if (category == null) return NotFound();

        filter.CategoryId = category.Id;
        var result = await _productService.GetProductsAsync(filter);
        var brands = await _brandService.GetAllBrandsAsync(true);
        ViewBag.Category = category;
        ViewBag.Brands = brands;
        ViewBag.Filter = filter;

        ViewData["MetaTitle"] = $"{category.Name} - Avansas";
        ViewData["MetaDescription"] = $"{category.Name} kategorisindeki ürünleri keşfedin. Avansas'ta uygun fiyat ve hızlı teslimat.";
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q, int page = 1)
    {
        var filter = new ProductFilterDto { SearchTerm = q, PageNumber = page, IsActive = true };
        var result = await _productService.GetProductsAsync(filter);
        ViewBag.SearchTerm = q;
        return View("Index", result);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(new List<object>());

        var results = await _productService.SearchSuggestionsAsync(q);
        return Json(results.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            slug = p.Slug,
            price = p.EffectivePrice.ToString("N2"),
            imageUrl = p.MainImageUrl ?? "/images/no-image.png",
            brandName = p.BrandName
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StockNotify(int productId, string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Lütfen geçerli bir e-posta adresi girin.";
        }
        else
        {
            await _stockNotificationService.SubscribeAsync(productId, email);
            TempData["Success"] = "Ürün stoğa girdiğinde size e-posta ile bildirim göndereceğiz.";
        }

        var product = await _productService.GetProductByIdAsync(productId);
        return Redirect($"/urun/{product?.Slug ?? ""}");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AskQuestion(int productId, string questionText)
    {
        if (string.IsNullOrWhiteSpace(questionText))
        {
            TempData["Error"] = "Lütfen bir soru yazın.";
        }
        else
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;
            var askerName = User.Identity?.IsAuthenticated == true
                ? User.Identity.Name ?? "Anonim"
                : "Misafir";

            var dto = new Application.DTOs.CreateQuestionDto { ProductId = productId, QuestionText = questionText };
            await _productQuestionService.AskQuestionAsync(userId, askerName, dto);
            TempData["Success"] = "Sorunuz başarıyla gönderildi. Onaylandıktan sonra yayınlanacaktır.";
        }

        var product = await _productService.GetProductByIdAsync(productId);
        return Redirect($"/urun/{product?.Slug ?? ""}#questions");
    }
}
