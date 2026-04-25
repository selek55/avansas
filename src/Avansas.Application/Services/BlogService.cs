using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class BlogService : IBlogService
{
    private readonly IUnitOfWork _uow;
    public BlogService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<BlogPostDto>> GetPublishedPostsAsync(int page = 1, int pageSize = 10, int? categoryId = null)
    {
        var query = _uow.BlogPosts.Query()
            .Include(p => p.Author).Include(p => p.BlogCategory)
            .Where(p => p.IsPublished && !p.IsDeleted);

        if (categoryId.HasValue) query = query.Where(p => p.BlogCategoryId == categoryId.Value);
        query = query.OrderByDescending(p => p.PublishedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<BlogPostDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total, PageNumber = page, PageSize = pageSize
        };
    }

    public async Task<BlogPostDto?> GetPostBySlugAsync(string slug)
    {
        var post = await _uow.BlogPosts.Query()
            .Include(p => p.Author).Include(p => p.BlogCategory)
            .FirstOrDefaultAsync(p => p.Slug == slug && !p.IsDeleted);
        if (post == null) return null;

        post.ViewCount++;
        post.UpdatedAt = DateTime.UtcNow;
        _uow.BlogPosts.Update(post);
        await _uow.SaveChangesAsync();

        return MapToDto(post);
    }

    public async Task<BlogPostDto?> GetPostByIdAsync(int id)
    {
        var post = await _uow.BlogPosts.Query()
            .Include(p => p.Author).Include(p => p.BlogCategory)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        return post == null ? null : MapToDto(post);
    }

    public async Task<List<BlogPostDto>> GetAllPostsAsync()
    {
        var posts = await _uow.BlogPosts.Query()
            .Include(p => p.Author).Include(p => p.BlogCategory)
            .Where(p => !p.IsDeleted).OrderByDescending(p => p.CreatedAt).ToListAsync();
        return posts.Select(MapToDto).ToList();
    }

    public async Task<int> CreatePostAsync(string authorId, CreateBlogPostDto dto)
    {
        var post = new BlogPost
        {
            Title = dto.Title, Slug = !string.IsNullOrWhiteSpace(dto.Slug) ? dto.Slug : ToSlug(dto.Title),
            Content = dto.Content, Summary = dto.Summary, ImageUrl = dto.ImageUrl,
            AuthorId = authorId, BlogCategoryId = dto.BlogCategoryId,
            IsPublished = dto.IsPublished,
            PublishedAt = dto.IsPublished ? DateTime.UtcNow : null,
            MetaTitle = dto.MetaTitle, MetaDescription = dto.MetaDescription
        };
        await _uow.BlogPosts.AddAsync(post);
        await _uow.SaveChangesAsync();
        return post.Id;
    }

    public async Task UpdatePostAsync(UpdateBlogPostDto dto)
    {
        var post = await _uow.BlogPosts.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException("Yazı bulunamadı");
        post.Title = dto.Title;
        post.Slug = !string.IsNullOrWhiteSpace(dto.Slug) ? dto.Slug : ToSlug(dto.Title);
        post.Content = dto.Content; post.Summary = dto.Summary;
        post.ImageUrl = dto.ImageUrl; post.BlogCategoryId = dto.BlogCategoryId;
        post.MetaTitle = dto.MetaTitle; post.MetaDescription = dto.MetaDescription;

        if (dto.IsPublished && !post.IsPublished) post.PublishedAt = DateTime.UtcNow;
        post.IsPublished = dto.IsPublished;
        post.UpdatedAt = DateTime.UtcNow;

        _uow.BlogPosts.Update(post);
        await _uow.SaveChangesAsync();
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await _uow.BlogPosts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Yazı bulunamadı");
        _uow.BlogPosts.SoftDelete(post);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<BlogCategoryDto>> GetCategoriesAsync()
    {
        var categories = await _uow.BlogCategories.Query()
            .Include(c => c.Posts)
            .Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync();
        return categories.Select(c => new BlogCategoryDto
        {
            Id = c.Id, Name = c.Name, Slug = c.Slug,
            Description = c.Description,
            PostCount = c.Posts.Count(p => p.IsPublished && !p.IsDeleted)
        }).ToList();
    }

    public async Task<int> CreateCategoryAsync(CreateBlogCategoryDto dto)
    {
        var category = new BlogCategory
        {
            Name = dto.Name,
            Slug = !string.IsNullOrWhiteSpace(dto.Slug) ? dto.Slug : ToSlug(dto.Name),
            Description = dto.Description
        };
        await _uow.BlogCategories.AddAsync(category);
        await _uow.SaveChangesAsync();
        return category.Id;
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _uow.BlogCategories.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Kategori bulunamadı");
        _uow.BlogCategories.SoftDelete(category);
        await _uow.SaveChangesAsync();
    }

    private static BlogPostDto MapToDto(BlogPost p) => new()
    {
        Id = p.Id, Title = p.Title, Slug = p.Slug,
        Content = p.Content, Summary = p.Summary, ImageUrl = p.ImageUrl,
        AuthorName = p.Author?.FullName ?? string.Empty,
        CategoryName = p.BlogCategory?.Name,
        BlogCategoryId = p.BlogCategoryId,
        IsPublished = p.IsPublished, PublishedAt = p.PublishedAt,
        ViewCount = p.ViewCount, MetaTitle = p.MetaTitle,
        MetaDescription = p.MetaDescription, CreatedAt = p.CreatedAt
    };

    private static string ToSlug(string text)
    {
        var slug = text.ToLowerInvariant()
            .Replace("ş", "s").Replace("ğ", "g").Replace("ü", "u")
            .Replace("ö", "o").Replace("ç", "c").Replace("ı", "i")
            .Replace("Ş", "s").Replace("Ğ", "g").Replace("Ü", "u")
            .Replace("Ö", "o").Replace("Ç", "c").Replace("İ", "i");

        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[\s-]+", "-").Trim('-');
        return slug;
    }
}
