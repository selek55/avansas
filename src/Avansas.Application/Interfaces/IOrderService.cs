using Avansas.Application.DTOs;
using Avansas.Domain.Enums;

namespace Avansas.Application.Interfaces;

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetOrdersAsync(int page = 1, int pageSize = 20, OrderStatus? status = null);
    Task<PagedResult<OrderDto>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 10);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto?> GetOrderByNumberAsync(string orderNumber);
    Task<int> CreateOrderFromCartAsync(CreateOrderDto dto);
    Task UpdateOrderStatusAsync(UpdateOrderStatusDto dto);
    Task CancelOrderAsync(int orderId, string userId);
    Task<decimal> CalculateShippingCostAsync(decimal cartTotal);
    Task<Dictionary<string, object>> GetOrderStatisticsAsync();
}
