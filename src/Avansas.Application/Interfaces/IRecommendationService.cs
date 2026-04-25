using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IRecommendationService
{
    /// <summary>Birlikte sıklıkla satın alınan ürünler (sipariş geçmişinden)</summary>
    Task<List<ProductListDto>> GetFrequentlyBoughtTogetherAsync(int productId, int count = 6);

    /// <summary>Aynı kategori/marka benzer ürünler</summary>
    Task<List<ProductListDto>> GetSimilarProductsAsync(int productId, int count = 6);

    /// <summary>Kullanıcı/session bazlı son görüntülenenler</summary>
    Task<List<ProductListDto>> GetRecentlyViewedAsync(string? userId, string? sessionId, int count = 10);

    /// <summary>Ürün görüntüleme kaydı (30 dk içinde tekrar kayıt yapılmaz)</summary>
    Task RecordProductViewAsync(int productId, string? userId, string? sessionId);

    /// <summary>Kategoride popüler ürünler</summary>
    Task<List<ProductListDto>> GetPopularInCategoryAsync(int categoryId, int count = 6);

    /// <summary>Kullanıcının satın alma + görüntüleme geçmişine göre kişiselleştirilmiş öneriler</summary>
    Task<List<ProductListDto>> GetPersonalizedRecommendationsAsync(string userId, int count = 8);
}
