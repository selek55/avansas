namespace Avansas.Domain.Entities;

public class GiftCard : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal RemainingBalance { get; set; }
    public string PurchaserUserId { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string? PersonalMessage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime ExpiresAt { get; set; }
    public string? RedeemedByUserId { get; set; }
    public DateTime? RedeemedAt { get; set; }

    public ApplicationUser Purchaser { get; set; } = null!;
    public ICollection<GiftCardTransaction> Transactions { get; set; } = new List<GiftCardTransaction>();
}
