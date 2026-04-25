using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Avansas.Web.Services;

public class CachedRecommendationService : IRecommendationService
{
    private readonly IRecommendationService _inner;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RecentTtl = TimeSpan.FromMinutes(5);

    public CachedRecommendationService(IRecommendationService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<List<ProductListDto>> GetFrequentlyBoughtTogetherAsync(int productId, int count = 6)
        => _cache.GetOrCreateAsync($"rec:bought-together:{productId}:{count}", e =>
        {
            e.AbsoluteExpirationRelativeToNow = Ttl;
            return _inner.GetFrequentlyBoughtTogetherAsync(productId, count);
        })!;

    public Task<List<ProductListDto>> GetSimilarProductsAsync(int productId, int count = 6)
        => _cache.GetOrCreateAsync($"rec:similar:{productId}:{count}", e =>
        {
            e.AbsoluteExpirationRelativeToNow = Ttl;
            return _inner.GetSimilarProductsAsync(productId, count);
        })!;

    public Task<List<ProductListDto>> GetRecentlyViewedAsync(string? userId, string? sessionId, int count = 10)
    {
        var key = $"rec:recent:{userId ?? sessionId}:{count}";
        return _cache.GetOrCreateAsync(key, e =>
        {
            e.AbsoluteExpirationRelativeToNow = RecentTtl;
            return _inner.GetRecentlyViewedAsync(userId, sessionId, count);
        })!;
    }

    // View kaydı önbelleğe alınmaz — doğrudan DB'e yazar
    public Task RecordProductViewAsync(int productId, string? userId, string? sessionId)
        => _inner.RecordProductViewAsync(productId, userId, sessionId);

    public Task<List<ProductListDto>> GetPopularInCategoryAsync(int categoryId, int count = 6)
        => _cache.GetOrCreateAsync($"rec:popular-cat:{categoryId}:{count}", e =>
        {
            e.AbsoluteExpirationRelativeToNow = Ttl;
            return _inner.GetPopularInCategoryAsync(categoryId, count);
        })!;

    public Task<List<ProductListDto>> GetPersonalizedRecommendationsAsync(string userId, int count = 8)
        => _cache.GetOrCreateAsync($"rec:personalized:{userId}:{count}", e =>
        {
            e.AbsoluteExpirationRelativeToNow = RecentTtl;
            return _inner.GetPersonalizedRecommendationsAsync(userId, count);
        })!;
}
