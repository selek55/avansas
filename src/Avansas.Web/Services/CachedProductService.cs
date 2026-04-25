using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Avansas.Web.Services;

public class CachedProductService : IProductService
{
    private readonly IProductService _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string FeaturedKey = "products_featured";
    private const string NewKey = "products_new";

    public CachedProductService(IProductService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug)
    {
        var key = $"product_slug_{slug}";
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetProductBySlugAsync(slug);
        });
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var key = $"product_{id}";
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetProductByIdAsync(id);
        });
    }

    public async Task<List<ProductListDto>> GetFeaturedProductsAsync(int count = 8)
    {
        return (await _cache.GetOrCreateAsync($"{FeaturedKey}_{count}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetFeaturedProductsAsync(count);
        }))!;
    }

    public async Task<List<ProductListDto>> GetNewProductsAsync(int count = 8)
    {
        return (await _cache.GetOrCreateAsync($"{NewKey}_{count}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetNewProductsAsync(count);
        }))!;
    }

    public async Task<List<ProductListDto>> GetRelatedProductsAsync(int productId, int count = 6)
    {
        return (await _cache.GetOrCreateAsync($"related_{productId}_{count}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetRelatedProductsAsync(productId, count);
        }))!;
    }

    public async Task<List<ProductListDto>> SearchSuggestionsAsync(string term, int count = 6)
        => await _inner.SearchSuggestionsAsync(term, count);

    public Task<PagedResult<ProductListDto>> GetProductsAsync(ProductFilterDto filter)
        => _inner.GetProductsAsync(filter);

    public Task<List<ProductListDto>> GetProductsByCategoryAsync(int categoryId, int count = 20)
        => _inner.GetProductsByCategoryAsync(categoryId, count);

    // Write operations — invalidate cache
    public async Task<int> CreateProductAsync(CreateProductDto dto)
    {
        var id = await _inner.CreateProductAsync(dto);
        InvalidateProductCaches();
        return id;
    }

    public async Task UpdateProductAsync(UpdateProductDto dto)
    {
        await _inner.UpdateProductAsync(dto);
        _cache.Remove($"product_{dto.Id}");
        InvalidateProductCaches();
    }

    public async Task DeleteProductAsync(int id)
    {
        await _inner.DeleteProductAsync(id);
        _cache.Remove($"product_{id}");
        InvalidateProductCaches();
    }

    public async Task UpdateStockAsync(int productId, int quantity)
    {
        await _inner.UpdateStockAsync(productId, quantity);
        _cache.Remove($"product_{productId}");
    }

    public Task<bool> IsSlugUniqueAsync(string slug, int? excludeId = null)
        => _inner.IsSlugUniqueAsync(slug, excludeId);

    public Task<List<ProductImageDto>> GetProductImagesAsync(int productId)
        => _inner.GetProductImagesAsync(productId);

    public async Task<ProductImageDto> AddProductImageAsync(int productId, string imageUrl, bool isMain = false)
    {
        var result = await _inner.AddProductImageAsync(productId, imageUrl, isMain);
        _cache.Remove($"product_{productId}");
        return result;
    }

    public async Task DeleteProductImageAsync(int imageId)
    {
        await _inner.DeleteProductImageAsync(imageId);
    }

    public async Task SetMainImageAsync(int productId, int imageId)
    {
        await _inner.SetMainImageAsync(productId, imageId);
        _cache.Remove($"product_{productId}");
    }

    public Task ReorderImagesAsync(int productId, List<int> orderedImageIds)
        => _inner.ReorderImagesAsync(productId, orderedImageIds);

    // Variant management — no caching, delegate directly
    public Task<List<ProductVariantDto>> GetProductVariantsAsync(int productId)
        => _inner.GetProductVariantsAsync(productId);

    public Task<int> AddVariantAsync(int productId, ProductVariantDto dto)
        => _inner.AddVariantAsync(productId, dto);

    public Task UpdateVariantAsync(ProductVariantDto dto)
        => _inner.UpdateVariantAsync(dto);

    public Task DeleteVariantAsync(int variantId)
        => _inner.DeleteVariantAsync(variantId);

    private void InvalidateProductCaches()
    {
        // Remove known list caches
        for (int i = 1; i <= 20; i++)
        {
            _cache.Remove($"{FeaturedKey}_{i}");
            _cache.Remove($"{NewKey}_{i}");
        }
    }
}
