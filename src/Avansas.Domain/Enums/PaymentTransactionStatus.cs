namespace Avansas.Domain.Enums;

public enum PaymentTransactionStatus
{
    Initiated = 0,
    Pending3DSecure = 1,
    Success = 2,
    Failed = 3,
    Refunded = 4,
    PartialRefund = 5,
    Cancelled = 6
}
