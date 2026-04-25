using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Avansas.Web.Controllers;

[Route("")]
public class SeoController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IBlogService _blogService;

    public SeoController(IProductService productService, ICategoryService categoryService, IBlogService blogService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _blogService = blogService;
    }

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Static pages
        var staticPages = new[] { "/", "/urunler", "/blog", "/destek", "/toplu-siparis", "/karsilastir" };
        foreach (var page in staticPages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}{page}</loc>");
            sb.AppendLine("    <changefreq>daily</changefreq>");
            sb.AppendLine($"    <priority>{(page == "/" ? "1.0" : "0.8")}</priority>");
            sb.AppendLine("  </url>");
        }

        // Product URLs
        var productResult = await _productService.GetProductsAsync(new ProductFilterDto { IsActive = true, PageSize = 10000 });
        foreach (var product in productResult.Items)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}/urun/{product.Slug}</loc>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.9</priority>");
            sb.AppendLine("  </url>");
        }

        // Category URLs
        var categories = await _categoryService.GetAllCategoriesAsync();
        foreach (var category in categories)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}/kategori/{category.Slug}</loc>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.8</priority>");
            sb.AppendLine("  </url>");
        }

        // Blog URLs
        var blogPosts = await _blogService.GetAllPostsAsync();
        foreach (var post in blogPosts)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}/blog/{post.Slug}</loc>");
            sb.AppendLine("    <changefreq>monthly</changefreq>");
            sb.AppendLine("    <priority>0.7</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 86400)]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();

        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Disallow: /admin/");
        sb.AppendLine("Disallow: /hesap/");
        sb.AppendLine("Disallow: /sepet/");
        sb.AppendLine("Disallow: /odeme/");
        sb.AppendLine("Disallow: /api/");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}
