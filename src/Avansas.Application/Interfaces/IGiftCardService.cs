namespace Avansas.Application.Interfaces;

public record PurchaseGiftCardDto(
    string PurchaserUserId,
    decimal Amount,
    string RecipientEmail,
    string? RecipientName,
    string? PersonalMessage);

public record GiftCardDto(
    int Id,
    string Code,
    decimal InitialBalance,
    decimal RemainingBalance,
    string RecipientEmail,
    string? RecipientName,
    bool IsActive,
    DateTime ExpiresAt,
    DateTime CreatedAt);

public interface IGiftCardService
{
    Task<GiftCardDto> PurchaseGiftCardAsync(PurchaseGiftCardDto dto);
    Task<GiftCardDto?> GetByCodeAsync(string code);
    Task<decimal> RedeemAsync(string code, string userId, int orderId, decimal amount);
    Task RefundAsync(int giftCardId, decimal amount, int orderId);
    Task<List<GiftCardDto>> GetUserGiftCardsAsync(string userId);
    Task<bool> ValidateCodeAsync(string code, decimal requiredAmount);
}
