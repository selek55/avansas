using Avansas.Application.DTOs;
using Avansas.Domain.Enums;

namespace Avansas.Application.Interfaces;

public interface ILoyaltyService
{
    Task<LoyaltySummaryDto> GetUserSummaryAsync(string userId);
    Task AddPointsAsync(string userId, int points, string description, LoyaltyType type, int? orderId = null);
    Task<bool> SpendPointsAsync(string userId, int points, string description, int? orderId = null);
    Task EarnOrderPointsAsync(string userId, decimal orderTotal, int orderId);
}
