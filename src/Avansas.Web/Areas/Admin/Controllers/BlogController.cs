using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Avansas.Domain.Entities;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BlogController : Controller
{
    private readonly IBlogService _blogService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BlogController(IBlogService blogService, UserManager<ApplicationUser> userManager)
    {
        _blogService = blogService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var posts = await _blogService.GetAllPostsAsync();
        return View(posts);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View(new CreateBlogPostDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBlogPostDto dto)
    {
        if (!ModelState.IsValid) { await PopulateCategoriesAsync(); return View(dto); }
        var userId = _userManager.GetUserId(User)!;
        await _blogService.CreatePostAsync(userId, dto);
        TempData["Success"] = "Blog yazısı oluşturuldu";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var post = await _blogService.GetPostByIdAsync(id);
        if (post == null) return NotFound();
        await PopulateCategoriesAsync();
        var dto = new UpdateBlogPostDto
        {
            Id = post.Id, Title = post.Title, Slug = post.Slug, Content = post.Content,
            Summary = post.Summary, ImageUrl = post.ImageUrl, BlogCategoryId = post.BlogCategoryId,
            IsPublished = post.IsPublished, MetaTitle = post.MetaTitle, MetaDescription = post.MetaDescription
        };
        return View(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UpdateBlogPostDto dto)
    {
        if (!ModelState.IsValid) { await PopulateCategoriesAsync(); return View(dto); }
        await _blogService.UpdatePostAsync(dto);
        TempData["Success"] = "Blog yazısı güncellendi";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _blogService.DeletePostAsync(id);
        TempData["Success"] = "Blog yazısı silindi";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync()
    {
        var categories = await _blogService.GetCategoriesAsync();
        ViewBag.BlogCategories = new SelectList(categories, "Id", "Name");
    }
}
