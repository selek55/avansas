using Avansas.Domain.Enums;

namespace Avansas.Domain.Entities;

public class GiftCardTransaction : BaseEntity
{
    public int GiftCardId { get; set; }
    public int? OrderId { get; set; }
    public decimal Amount { get; set; }
    public GiftCardTransactionType Type { get; set; }
    public decimal BalanceAfter { get; set; }

    public GiftCard GiftCard { get; set; } = null!;
    public Order? Order { get; set; }
}
