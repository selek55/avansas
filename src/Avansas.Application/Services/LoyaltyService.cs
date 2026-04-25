using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class LoyaltyService : ILoyaltyService
{
    private readonly IUnitOfWork _uow;
    public LoyaltyService(IUnitOfWork uow) => _uow = uow;

    public async Task<LoyaltySummaryDto> GetUserSummaryAsync(string userId)
    {
        var transactions = await _uow.LoyaltyTransactions.Query()
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var earned = transactions.Where(t => t.Points > 0).Sum(t => t.Points);
        var spent = transactions.Where(t => t.Points < 0).Sum(t => t.Points);

        return new LoyaltySummaryDto
        {
            TotalPoints = transactions.Sum(t => t.Points),
            EarnedPoints = earned,
            SpentPoints = spent,
            RecentTransactions = transactions.Take(10).Select(MapToDto).ToList()
        };
    }

    public async Task AddPointsAsync(string userId, int points, string description, LoyaltyType type, int? orderId = null)
    {
        var transaction = new LoyaltyTransaction
        {
            UserId = userId, Points = Math.Abs(points),
            Description = description, Type = type, OrderId = orderId
        };
        await _uow.LoyaltyTransactions.AddAsync(transaction);
        await _uow.SaveChangesAsync();
    }

    public async Task<bool> SpendPointsAsync(string userId, int points, string description, int? orderId = null)
    {
        var totalPoints = await _uow.LoyaltyTransactions.Query()
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .SumAsync(t => t.Points);

        if (totalPoints < points) return false;

        var transaction = new LoyaltyTransaction
        {
            UserId = userId, Points = -Math.Abs(points),
            Description = description, Type = LoyaltyType.OrderSpend, OrderId = orderId
        };
        await _uow.LoyaltyTransactions.AddAsync(transaction);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task EarnOrderPointsAsync(string userId, decimal orderTotal, int orderId)
    {
        var points = (int)(orderTotal / 10);
        if (points <= 0) return;

        await AddPointsAsync(userId, points, $"Sipariş #{orderId} puanı", LoyaltyType.OrderEarn, orderId);
    }

    private static string GetTypeText(LoyaltyType type) => type switch
    {
        LoyaltyType.OrderEarn => "Sipariş Puanı",
        LoyaltyType.OrderSpend => "Puan Kullanımı",
        LoyaltyType.Welcome => "Hoş Geldin",
        LoyaltyType.ReviewBonus => "Yorum Bonusu",
        LoyaltyType.Refund => "İade",
        LoyaltyType.AdminAdjust => "Admin Düzeltmesi",
        _ => type.ToString()
    };

    private static LoyaltyTransactionDto MapToDto(LoyaltyTransaction t) => new()
    {
        Id = t.Id, Points = t.Points, Description = t.Description,
        Type = t.Type, TypeText = GetTypeText(t.Type),
        OrderId = t.OrderId, CreatedAt = t.CreatedAt
    };
}
