using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Avansas.Web.Services;

public class CachedCategoryService : ICategoryService
{
    private readonly ICategoryService _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string AllKey = "categories_all";
    private const string RootKey = "categories_root";

    public CachedCategoryService(ICategoryService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        return (await _cache.GetOrCreateAsync(AllKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetAllCategoriesAsync();
        }))!;
    }

    public async Task<List<CategoryDto>> GetRootCategoriesAsync()
    {
        return (await _cache.GetOrCreateAsync(RootKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetRootCategoriesAsync();
        }))!;
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        return await _cache.GetOrCreateAsync($"category_{id}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetCategoryByIdAsync(id);
        });
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug)
    {
        return await _cache.GetOrCreateAsync($"category_slug_{slug}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetCategoryBySlugAsync(slug);
        });
    }

    public Task<List<CategoryDto>> GetSubCategoriesAsync(int parentId)
        => _inner.GetSubCategoriesAsync(parentId);

    // Write operations — invalidate
    public async Task<int> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var id = await _inner.CreateCategoryAsync(dto);
        InvalidateCategoryCaches();
        return id;
    }

    public async Task UpdateCategoryAsync(UpdateCategoryDto dto)
    {
        await _inner.UpdateCategoryAsync(dto);
        _cache.Remove($"category_{dto.Id}");
        InvalidateCategoryCaches();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        await _inner.DeleteCategoryAsync(id);
        _cache.Remove($"category_{id}");
        InvalidateCategoryCaches();
    }

    private void InvalidateCategoryCaches()
    {
        _cache.Remove(AllKey);
        _cache.Remove(RootKey);
    }
}
