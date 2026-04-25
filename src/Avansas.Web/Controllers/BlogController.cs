// =====================================================================
// Program.cs'e eklenecek route satırları:
//
// app.MapControllerRoute(name: "blog-detail", pattern: "blog/{slug}", defaults: new { controller = "Blog", action = "Detail" });
// app.MapControllerRoute(name: "blog", pattern: "blog", defaults: new { controller = "Blog", action = "Index" });
// app.MapControllerRoute(name: "destek", pattern: "destek", defaults: new { controller = "Support", action = "Index" });
// app.MapControllerRoute(name: "destek-olustur", pattern: "destek/olustur", defaults: new { controller = "Support", action = "Create" });
// app.MapControllerRoute(name: "destek-detay", pattern: "destek/{id:int}", defaults: new { controller = "Support", action = "Detail" });
// app.MapControllerRoute(name: "karsilastir", pattern: "karsilastir", defaults: new { controller = "Compare", action = "Index" });
// =====================================================================

using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Controllers;

[Route("blog")]
public class BlogController : Controller
{
    private readonly IBlogService _blogService;

    public BlogController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, int? categoryId = null)
    {
        var posts = await _blogService.GetPublishedPostsAsync(page, 9, categoryId);
        var categories = await _blogService.GetCategoriesAsync();
        ViewBag.Categories = categories;
        ViewBag.SelectedCategoryId = categoryId;
        return View(posts);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var post = await _blogService.GetPostBySlugAsync(slug);
        if (post == null) return NotFound();

        var categories = await _blogService.GetCategoriesAsync();
        ViewBag.Categories = categories;

        ViewData["MetaTitle"] = !string.IsNullOrEmpty(post.MetaTitle) ? post.MetaTitle : $"{post.Title} - Avansas Blog";
        ViewData["MetaDescription"] = !string.IsNullOrEmpty(post.MetaDescription) ? post.MetaDescription : post.Summary;
        ViewData["MetaImage"] = post.ImageUrl;
        return View(post);
    }
}
