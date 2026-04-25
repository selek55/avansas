using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class StockNotificationService : IStockNotificationService
{
    private readonly IUnitOfWork _uow;
    public StockNotificationService(IUnitOfWork uow) => _uow = uow;

    public async Task SubscribeAsync(int productId, string email)
    {
        var exists = await _uow.StockNotifications.Query()
            .AnyAsync(s => s.ProductId == productId && s.Email == email && !s.IsNotified && !s.IsDeleted);
        if (exists) return;

        await _uow.StockNotifications.AddAsync(new StockNotification
        {
            ProductId = productId, Email = email, IsNotified = false
        });
        await _uow.SaveChangesAsync();
    }

    public async Task NotifyInStockAsync(int productId)
    {
        var subscriptions = await _uow.StockNotifications.Query()
            .Where(s => s.ProductId == productId && !s.IsNotified && !s.IsDeleted)
            .ToListAsync();

        foreach (var sub in subscriptions)
        {
            sub.IsNotified = true;
            sub.UpdatedAt = DateTime.UtcNow;
            _uow.StockNotifications.Update(sub);
            // Actual email sending would use IEmailService
        }
        await _uow.SaveChangesAsync();
    }

    public async Task<List<StockNotificationDto>> GetSubscriptionsAsync(int productId)
    {
        var subs = await _uow.StockNotifications.Query()
            .Include(s => s.Product)
            .Where(s => s.ProductId == productId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        return subs.Select(MapToDto).ToList();
    }

    private static StockNotificationDto MapToDto(StockNotification s) => new()
    {
        Id = s.Id, ProductId = s.ProductId,
        ProductName = s.Product?.Name ?? string.Empty,
        Email = s.Email, IsNotified = s.IsNotified,
        CreatedAt = s.CreatedAt
    };
}
