using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;

    public CategoryService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _uow.Categories.Query()
            .Include(c => c.SubCategories)
            .Include(c => c.Products)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();
        return categories.Select(MapToDto).ToList();
    }

    public async Task<List<CategoryDto>> GetRootCategoriesAsync()
    {
        var categories = await _uow.Categories.Query()
            .Include(c => c.SubCategories.Where(s => s.IsActive && !s.IsDeleted))
            .Include(c => c.Products)
            .Where(c => c.ParentCategoryId == null && c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();
        return categories.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await _uow.Categories.Query()
            .Include(c => c.SubCategories).Include(c => c.ParentCategory).Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        return category == null ? null : MapToDto(category);
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug)
    {
        var category = await _uow.Categories.Query()
            .Include(c => c.SubCategories).Include(c => c.ParentCategory).Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted);
        return category == null ? null : MapToDto(category);
    }

    public async Task<List<CategoryDto>> GetSubCategoriesAsync(int parentId)
    {
        var categories = await _uow.Categories.Query()
            .Include(c => c.Products)
            .Where(c => c.ParentCategoryId == parentId && c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder).ToListAsync();
        return categories.Select(MapToDto).ToList();
    }

    public async Task<int> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var slug = !string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Slug) : GenerateSlug(dto.Name);
        var category = new Category
        {
            Name = dto.Name, Slug = slug, Description = dto.Description,
            ImageUrl = dto.ImageUrl, ParentCategoryId = dto.ParentCategoryId,
            DisplayOrder = dto.DisplayOrder, IsActive = dto.IsActive
        };
        await _uow.Categories.AddAsync(category);
        await _uow.SaveChangesAsync();
        return category.Id;
    }

    public async Task UpdateCategoryAsync(UpdateCategoryDto dto)
    {
        var category = await _uow.Categories.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Category {dto.Id} not found");

        if (!string.IsNullOrWhiteSpace(dto.Slug)) category.Slug = GenerateSlug(dto.Slug);
        category.Name = dto.Name; category.Description = dto.Description;
        category.ImageUrl = dto.ImageUrl; category.ParentCategoryId = dto.ParentCategoryId;
        category.DisplayOrder = dto.DisplayOrder; category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        _uow.Categories.Update(category);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _uow.Categories.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found");
        _uow.Categories.SoftDelete(category);
        await _uow.SaveChangesAsync();
    }

    private static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex.Replace(
            name.ToLower().Replace(" ", "-").Replace("ş", "s").Replace("ğ", "g")
                .Replace("ü", "u").Replace("ö", "o").Replace("ç", "c").Replace("ı", "i"),
            @"[^a-z0-9\-]", "");

    private static CategoryDto MapToDto(Category c) => new()
    {
        Id = c.Id, Name = c.Name, Slug = c.Slug, Description = c.Description,
        ImageUrl = c.ImageUrl, ParentCategoryId = c.ParentCategoryId,
        ParentCategoryName = c.ParentCategory?.Name, DisplayOrder = c.DisplayOrder,
        IsActive = c.IsActive, ProductCount = c.Products.Count(p => !p.IsDeleted),
        SubCategories = c.SubCategories.Where(s => !s.IsDeleted).Select(MapToDto).ToList()
    };
}
