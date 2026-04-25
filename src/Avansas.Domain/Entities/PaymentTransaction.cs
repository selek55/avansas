using Avansas.Domain.Enums;

namespace Avansas.Domain.Entities;

public class PaymentTransaction : BaseEntity
{
    public int OrderId { get; set; }
    public string? TransactionId { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public int Installment { get; set; } = 1;
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Initiated;
    public PaymentMethod PaymentMethod { get; set; }
    public string? CardAssociation { get; set; }
    public string? CardFamily { get; set; }
    public string? CardLastFour { get; set; }
    public string? BinNumber { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int? FraudStatus { get; set; }
    public string? Token { get; set; }

    public Order Order { get; set; } = null!;
}
