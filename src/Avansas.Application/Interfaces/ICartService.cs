using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface ICartService
{
    Task<CartDto?> GetCartAsync(string? userId, string? sessionId);
    Task<CartDto> AddToCartAsync(string? userId, string? sessionId, AddToCartDto dto);
    Task<CartDto> UpdateCartItemAsync(string? userId, string? sessionId, UpdateCartItemDto dto);
    Task<CartDto> RemoveFromCartAsync(string? userId, string? sessionId, int cartItemId);
    Task<CartDto> ApplyCouponAsync(string? userId, string? sessionId, string couponCode);
    Task RemoveCouponAsync(string? userId, string? sessionId);
    Task ClearCartAsync(string? userId, string? sessionId);
    Task MergeGuestCartAsync(string sessionId, string userId);
    Task<int> GetCartItemCountAsync(string? userId, string? sessionId);
    Task ReorderFromOrderAsync(string userId, int orderId);
}
