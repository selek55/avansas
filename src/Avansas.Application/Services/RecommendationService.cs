using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IUnitOfWork _unitOfWork;

    public RecommendationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductListDto>> GetFrequentlyBoughtTogetherAsync(int productId, int count = 6)
    {
        // Aynı siparişlerde hangi ürünler bir arada satın alındı?
        var orderIds = await _unitOfWork.OrderItems.Query()
            .Where(i => i.ProductId == productId)
            .Select(i => i.OrderId)
            .Distinct()
            .ToListAsync();

        if (!orderIds.Any())
            return await GetSimilarProductsAsync(productId, count);

        var coProductIds = await _unitOfWork.OrderItems.Query()
            .Where(i => orderIds.Contains(i.OrderId) && i.ProductId != productId)
            .GroupBy(i => i.ProductId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(count)
            .ToListAsync();

        return await GetProductListDtos(coProductIds);
    }

    public async Task<List<ProductListDto>> GetSimilarProductsAsync(int productId, int count = 6)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null) return new List<ProductListDto>();

        var similar = await _unitOfWork.Products.Query()
            .Where(p => p.Id != productId && p.StockQuantity > 0
                        && (p.CategoryId == product.CategoryId || p.BrandId == product.BrandId))
            .OrderByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => MapToListDto(p))
            .ToListAsync();

        return similar;
    }

    public async Task<List<ProductListDto>> GetRecentlyViewedAsync(string? userId, string? sessionId, int count = 10)
    {
        IQueryable<ProductView> query = _unitOfWork.ProductViews.Query()
            .Include(v => v.Product);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(v => v.UserId == userId);
        else if (!string.IsNullOrEmpty(sessionId))
            query = query.Where(v => v.SessionId == sessionId);
        else
            return new List<ProductListDto>();

        var productIds = await query
            .OrderByDescending(v => v.ViewedAt)
            .Select(v => v.ProductId)
            .Distinct()
            .Take(count)
            .ToListAsync();

        return await GetProductListDtos(productIds);
    }

    public async Task RecordProductViewAsync(int productId, string? userId, string? sessionId)
    {
        var threshold = DateTime.UtcNow.AddMinutes(-30);

        // 30 dakika içinde aynı ürün zaten kaydedilmişse tekrar kayıt yapma
        var exists = await _unitOfWork.ProductViews.Query()
            .AnyAsync(v => v.ProductId == productId
                          && (userId != null ? v.UserId == userId : v.SessionId == sessionId)
                          && v.CreatedAt >= threshold);

        if (exists) return;

        await _unitOfWork.ProductViews.AddAsync(new ProductView
        {
            ProductId = productId,
            UserId = userId,
            SessionId = sessionId,
            ViewedAt = DateTime.UtcNow
        });

        // ViewCount'u artır
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product != null)
        {
            product.ViewCount++;
            _unitOfWork.Products.Update(product);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<ProductListDto>> GetPopularInCategoryAsync(int categoryId, int count = 6)
    {
        return await _unitOfWork.Products.Query()
            .Where(p => p.CategoryId == categoryId && p.StockQuantity > 0)
            .OrderByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => MapToListDto(p))
            .ToListAsync();
    }

    public async Task<List<ProductListDto>> GetPersonalizedRecommendationsAsync(string userId, int count = 8)
    {
        // Kullanıcının satın aldığı kategorileri bul
        var purchasedCategoryIds = await _unitOfWork.OrderItems.Query()
            .Include(i => i.Product)
            .Where(i => i.Order.UserId == userId)
            .Select(i => i.Product.CategoryId)
            .Distinct()
            .ToListAsync();

        // Kullanıcının görüntülediği kategorileri ekle
        var viewedCategoryIds = await _unitOfWork.ProductViews.Query()
            .Include(v => v.Product)
            .Where(v => v.UserId == userId)
            .Select(v => v.Product.CategoryId)
            .Distinct()
            .ToListAsync();

        var categoryIds = purchasedCategoryIds.Union(viewedCategoryIds).ToList();

        if (!categoryIds.Any())
        {
            // Hiç geçmiş yoksa en popüler ürünleri göster
            return await _unitOfWork.Products.Query()
                .Where(p => p.StockQuantity > 0)
                .OrderByDescending(p => p.ViewCount)
                .Take(count)
                .Select(p => MapToListDto(p))
                .ToListAsync();
        }

        // Kullanıcının zaten satın almadığı, bu kategorilerdeki popüler ürünler
        var purchasedProductIds = await _unitOfWork.OrderItems.Query()
            .Where(i => i.Order.UserId == userId)
            .Select(i => i.ProductId)
            .Distinct()
            .ToListAsync();

        return await _unitOfWork.Products.Query()
            .Where(p => categoryIds.Contains(p.CategoryId)
                        && !purchasedProductIds.Contains(p.Id)
                        && p.StockQuantity > 0)
            .OrderByDescending(p => p.ViewCount)
            .Take(count)
            .Select(p => MapToListDto(p))
            .ToListAsync();
    }

    private async Task<List<ProductListDto>> GetProductListDtos(List<int> productIds)
    {
        var products = await _unitOfWork.Products.Query()
            .Where(p => productIds.Contains(p.Id) && p.StockQuantity > 0)
            .Select(p => MapToListDto(p))
            .ToListAsync();

        // Orijinal sıralamayı koru
        return productIds.Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Select(p => p!)
            .ToList();
    }

    private static ProductListDto MapToListDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        Price = p.Price,
        DiscountedPrice = p.DiscountedPrice,
        MainImageUrl = p.MainImageUrl,
        StockQuantity = p.StockQuantity,
        CategoryId = p.CategoryId,
        BrandId = p.BrandId ?? 0
    };
}
