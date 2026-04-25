using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IStockNotificationService
{
    Task SubscribeAsync(int productId, string email);
    Task NotifyInStockAsync(int productId);
    Task<List<StockNotificationDto>> GetSubscriptionsAsync(int productId);
}
