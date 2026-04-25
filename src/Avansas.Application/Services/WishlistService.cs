using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _uow;

    public WishlistService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<ProductListDto>> GetWishlistAsync(string userId)
    {
        var items = await _uow.WishlistItems.Query()
            .Include(w => w.Product).ThenInclude(p => p.Brand)
            .Where(w => w.UserId == userId && !w.IsDeleted && !w.Product.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return items.Select(w => MapProductToListDto(w.Product)).ToList();
    }

    public async Task AddToWishlistAsync(string userId, int productId)
    {
        var exists = await _uow.WishlistItems.Query()
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId && !w.IsDeleted);
        if (exists) return;

        var item = new WishlistItem { UserId = userId, ProductId = productId };
        await _uow.WishlistItems.AddAsync(item);
        await _uow.SaveChangesAsync();
    }

    public async Task RemoveFromWishlistAsync(string userId, int productId)
    {
        var item = await _uow.WishlistItems.Query()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId && !w.IsDeleted);
        if (item == null) return;
        _uow.WishlistItems.SoftDelete(item);
        await _uow.SaveChangesAsync();
    }

    public async Task<bool> IsInWishlistAsync(string userId, int productId) =>
        await _uow.WishlistItems.Query()
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId && !w.IsDeleted);

    public async Task<int> GetWishlistCountAsync(string userId) =>
        await _uow.WishlistItems.Query()
            .CountAsync(w => w.UserId == userId && !w.IsDeleted);

    private static ProductListDto MapProductToListDto(Product p) => new()
    {
        Id = p.Id, Name = p.Name, Slug = p.Slug,
        Price = p.Price, DiscountedPrice = p.DiscountedPrice,
        MainImageUrl = p.MainImageUrl, StockQuantity = p.StockQuantity,
        BrandName = p.Brand?.Name,
        IsActive = p.IsActive, IsFeatured = p.IsFeatured
    };
}
