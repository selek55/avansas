using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IWishlistService
{
    Task<List<ProductListDto>> GetWishlistAsync(string userId);
    Task AddToWishlistAsync(string userId, int productId);
    Task RemoveFromWishlistAsync(string userId, int productId);
    Task<bool> IsInWishlistAsync(string userId, int productId);
    Task<int> GetWishlistCountAsync(string userId);
}
