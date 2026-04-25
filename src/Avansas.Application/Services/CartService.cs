using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;

    public CartService(IUnitOfWork uow) => _uow = uow;

    public async Task<CartDto?> GetCartAsync(string? userId, string? sessionId)
    {
        var cart = await GetOrNullCartAsync(userId, sessionId);
        return cart == null ? null : MapToDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(string? userId, string? sessionId, AddToCartDto dto)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);
        var product = await _uow.Products.GetByIdAsync(dto.ProductId)
            ?? throw new KeyNotFoundException("Ürün bulunamadı");

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId && !i.IsDeleted);
        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
            _uow.CartItems.Update(existingItem);
        }
        else
        {
            var item = new CartItem
            {
                CartId = cart.Id, ProductId = dto.ProductId,
                Quantity = dto.Quantity, UnitPrice = product.GetEffectivePrice()
            };
            await _uow.CartItems.AddAsync(item);
            cart.Items.Add(item);
        }

        await _uow.SaveChangesAsync();
        return await RefreshCartDto(cart.Id);
    }

    public async Task<CartDto> UpdateCartItemAsync(string? userId, string? sessionId, UpdateCartItemDto dto)
    {
        var cart = await GetOrNullCartAsync(userId, sessionId)
            ?? throw new InvalidOperationException("Sepet bulunamadı");

        var item = cart.Items.FirstOrDefault(i => i.Id == dto.CartItemId && !i.IsDeleted)
            ?? throw new KeyNotFoundException("Sepet kalemi bulunamadı");

        if (dto.Quantity <= 0) { _uow.CartItems.SoftDelete(item); }
        else { item.Quantity = dto.Quantity; item.UpdatedAt = DateTime.UtcNow; _uow.CartItems.Update(item); }

        await _uow.SaveChangesAsync();
        return await RefreshCartDto(cart.Id);
    }

    public async Task<CartDto> RemoveFromCartAsync(string? userId, string? sessionId, int cartItemId)
    {
        var cart = await GetOrNullCartAsync(userId, sessionId)
            ?? throw new InvalidOperationException("Sepet bulunamadı");

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new KeyNotFoundException("Sepet kalemi bulunamadı");

        _uow.CartItems.SoftDelete(item);
        await _uow.SaveChangesAsync();
        return await RefreshCartDto(cart.Id);
    }

    public async Task<CartDto> ApplyCouponAsync(string? userId, string? sessionId, string couponCode)
    {
        var cart = await GetOrNullCartAsync(userId, sessionId)
            ?? throw new InvalidOperationException("Sepet bulunamadı");

        var coupon = await _uow.Coupons.Query()
            .FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive && !c.IsDeleted
                && (c.ValidTo == null || c.ValidTo >= DateTime.UtcNow)
                && (c.UsageLimit == null || c.UsedCount < c.UsageLimit));

        if (coupon == null) throw new InvalidOperationException("Geçersiz veya süresi dolmuş kupon");

        var subTotal = cart.Items.Where(i => !i.IsDeleted).Sum(i => i.TotalPrice);
        if (coupon.MinOrderAmount.HasValue && subTotal < coupon.MinOrderAmount.Value)
            throw new InvalidOperationException($"Bu kuponu kullanmak için minimum {coupon.MinOrderAmount:C2} tutarında sipariş vermelisiniz");

        cart.CouponCode = couponCode;
        cart.DiscountAmount = coupon.DiscountType == Domain.Enums.DiscountType.Percentage
            ? Math.Min(subTotal * coupon.DiscountValue / 100, coupon.MaxDiscountAmount ?? decimal.MaxValue)
            : Math.Min(coupon.DiscountValue, coupon.MaxDiscountAmount ?? coupon.DiscountValue);

        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync();
        return await RefreshCartDto(cart.Id);
    }

    public async Task RemoveCouponAsync(string? userId, string? sessionId)
    {
        var cart = await GetOrNullCartAsync(userId, sessionId);
        if (cart == null) return;
        cart.CouponCode = null; cart.DiscountAmount = 0;
        _uow.Carts.Update(cart); await _uow.SaveChangesAsync();
    }

    public async Task ClearCartAsync(string? userId, string? sessionId)
    {
        var cart = await GetOrNullCartAsync(userId, sessionId);
        if (cart == null) return;
        foreach (var item in cart.Items.Where(i => !i.IsDeleted)) _uow.CartItems.SoftDelete(item);
        cart.CouponCode = null; cart.DiscountAmount = 0;
        _uow.Carts.Update(cart); await _uow.SaveChangesAsync();
    }

    public async Task MergeGuestCartAsync(string sessionId, string userId)
    {
        var guestCart = await GetOrNullCartAsync(null, sessionId);
        if (guestCart == null || !guestCart.Items.Any(i => !i.IsDeleted)) return;

        var userCart = await GetOrCreateCartAsync(userId, null);
        foreach (var item in guestCart.Items.Where(i => !i.IsDeleted))
        {
            var existing = userCart.Items.FirstOrDefault(i => i.ProductId == item.ProductId && !i.IsDeleted);
            if (existing != null) { existing.Quantity += item.Quantity; _uow.CartItems.Update(existing); }
            else { item.CartId = userCart.Id; _uow.CartItems.Update(item); }
        }
        _uow.Carts.SoftDelete(guestCart);
        await _uow.SaveChangesAsync();
    }

    public async Task<int> GetCartItemCountAsync(string? userId, string? sessionId)
    {
        var cart = await GetOrNullCartAsync(userId, sessionId);
        return cart?.Items.Where(i => !i.IsDeleted).Sum(i => i.Quantity) ?? 0;
    }

    private async Task<Cart?> GetOrNullCartAsync(string? userId, string? sessionId)
    {
        var query = _uow.Carts.Query()
            .Include(c => c.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Product)
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(userId)) return await query.FirstOrDefaultAsync(c => c.UserId == userId);
        if (!string.IsNullOrEmpty(sessionId)) return await query.FirstOrDefaultAsync(c => c.SessionId == sessionId);
        return null;
    }

    private async Task<Cart> GetOrCreateCartAsync(string? userId, string? sessionId)
    {
        var cart = await GetOrNullCartAsync(userId, sessionId);
        if (cart != null) return cart;

        cart = new Cart { UserId = userId, SessionId = sessionId };
        await _uow.Carts.AddAsync(cart);
        await _uow.SaveChangesAsync();
        cart.Items = new List<CartItem>();
        return cart;
    }

    private async Task<CartDto> RefreshCartDto(int cartId)
    {
        var cart = await _uow.Carts.Query()
            .Include(c => c.Items.Where(i => !i.IsDeleted)).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);
        return MapToDto(cart!);
    }

    public async Task ReorderFromOrderAsync(string userId, int orderId)
    {
        var order = await _uow.Orders.Query()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId)
            ?? throw new InvalidOperationException("Sipariş bulunamadı.");

        var skippedItems = new List<string>();

        foreach (var item in order.Items.Where(i => !i.IsDeleted))
        {
            if (item.Product == null || !item.Product.IsActive || item.Product.StockQuantity <= 0)
            {
                skippedItems.Add(item.ProductName);
                continue;
            }

            var addDto = new AddToCartDto
            {
                ProductId = item.ProductId,
                Quantity = Math.Min(item.Quantity, item.Product.StockQuantity)
            };
            await AddToCartAsync(userId, null, addDto);
        }

        if (skippedItems.Any())
            throw new InvalidOperationException(
                $"Şu ürünler stokta olmadığından eklenemedi: {string.Join(", ", skippedItems)}");
    }

    private static CartDto MapToDto(Cart cart) => new()
    {
        Id = cart.Id,
        CouponCode = cart.CouponCode,
        DiscountAmount = cart.DiscountAmount,
        SubTotal = cart.SubTotal,
        Total = cart.Total,
        ItemCount = cart.Items.Where(i => !i.IsDeleted).Sum(i => i.Quantity),
        Items = cart.Items.Where(i => !i.IsDeleted).Select(i => new CartItemDto
        {
            Id = i.Id, ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            ProductSlug = i.Product?.Slug ?? string.Empty,
            ProductImageUrl = i.Product?.MainImageUrl,
            ProductSKU = i.Product?.SKU,
            Quantity = i.Quantity, UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice, StockQuantity = i.Product?.StockQuantity ?? 0
        }).ToList()
    };
}
