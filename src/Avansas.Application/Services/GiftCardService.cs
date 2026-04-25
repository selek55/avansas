using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class GiftCardService : IGiftCardService
{
    private readonly IUnitOfWork _unitOfWork;

    public GiftCardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GiftCardDto> PurchaseGiftCardAsync(PurchaseGiftCardDto dto)
    {
        var code = GenerateCode();
        var card = new GiftCard
        {
            Code = code,
            InitialBalance = dto.Amount,
            RemainingBalance = dto.Amount,
            PurchaserUserId = dto.PurchaserUserId,
            RecipientEmail = dto.RecipientEmail,
            RecipientName = dto.RecipientName,
            PersonalMessage = dto.PersonalMessage,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        await _unitOfWork.GiftCards.AddAsync(card);

        var tx = new GiftCardTransaction
        {
            Amount = dto.Amount,
            Type = GiftCardTransactionType.Purchase,
            BalanceAfter = dto.Amount
        };
        await _unitOfWork.GiftCardTransactions.AddAsync(tx);
        await _unitOfWork.SaveChangesAsync();

        tx.GiftCardId = card.Id;
        _unitOfWork.GiftCardTransactions.Update(tx);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(card);
    }

    public async Task<GiftCardDto?> GetByCodeAsync(string code)
    {
        var card = await _unitOfWork.GiftCards.Query()
            .FirstOrDefaultAsync(g => g.Code == code.ToUpper());
        return card == null ? null : MapToDto(card);
    }

    public async Task<bool> ValidateCodeAsync(string code, decimal requiredAmount)
    {
        var card = await _unitOfWork.GiftCards.Query()
            .FirstOrDefaultAsync(g => g.Code == code.ToUpper());

        return card != null
               && card.IsActive
               && card.ExpiresAt > DateTime.UtcNow
               && card.RemainingBalance >= requiredAmount;
    }

    public async Task<decimal> RedeemAsync(string code, string userId, int orderId, decimal amount)
    {
        var card = await _unitOfWork.GiftCards.Query()
            .FirstOrDefaultAsync(g => g.Code == code.ToUpper())
            ?? throw new InvalidOperationException("Geçersiz hediye kartı kodu.");

        if (!card.IsActive || card.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Bu hediye kartı artık geçerli değil.");

        var actualAmount = Math.Min(amount, card.RemainingBalance);
        card.RemainingBalance -= actualAmount;
        card.RedeemedByUserId = userId;
        card.RedeemedAt = DateTime.UtcNow;

        if (card.RemainingBalance <= 0)
            card.IsActive = false;

        _unitOfWork.GiftCards.Update(card);

        var tx = new GiftCardTransaction
        {
            GiftCardId = card.Id,
            OrderId = orderId,
            Amount = actualAmount,
            Type = GiftCardTransactionType.Redemption,
            BalanceAfter = card.RemainingBalance
        };
        await _unitOfWork.GiftCardTransactions.AddAsync(tx);
        await _unitOfWork.SaveChangesAsync();

        return actualAmount;
    }

    public async Task RefundAsync(int giftCardId, decimal amount, int orderId)
    {
        var card = await _unitOfWork.GiftCards.GetByIdAsync(giftCardId)
            ?? throw new InvalidOperationException("Hediye kartı bulunamadı.");

        card.RemainingBalance += amount;
        card.IsActive = true;
        _unitOfWork.GiftCards.Update(card);

        var tx = new GiftCardTransaction
        {
            GiftCardId = giftCardId,
            OrderId = orderId,
            Amount = amount,
            Type = GiftCardTransactionType.Refund,
            BalanceAfter = card.RemainingBalance
        };
        await _unitOfWork.GiftCardTransactions.AddAsync(tx);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<GiftCardDto>> GetUserGiftCardsAsync(string userId)
    {
        var cards = await _unitOfWork.GiftCards.Query()
            .Where(g => g.PurchaserUserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return cards.Select(MapToDto).ToList();
    }

    private static GiftCardDto MapToDto(GiftCard g) => new(
        g.Id, g.Code, g.InitialBalance, g.RemainingBalance,
        g.RecipientEmail, g.RecipientName, g.IsActive, g.ExpiresAt, g.CreatedAt);

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new Random();
        return string.Concat(Enumerable.Range(0, 16).Select(_ => chars[rng.Next(chars.Length)]));
    }
}
