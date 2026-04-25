using Avansas.Domain.Entities;

namespace Avansas.Application.Interfaces;

public record InitiatePaymentDto(
    int OrderId,
    string UserId,
    string UserIp,
    string CardHolderName,
    string CardNumber,
    string ExpireMonth,
    string ExpireYear,
    string Cvc,
    int Installment = 1,
    bool Use3DSecure = true);

public record PaymentInitResult(
    bool Success,
    string? ConversationId,
    string? HtmlContent,
    string? ErrorMessage,
    int? PaymentTransactionId = null);

public record PaymentResult(
    bool Success,
    int OrderId,
    string? PaymentId,
    decimal PaidAmount,
    string? ErrorCode,
    string? ErrorMessage);

public record RefundResult(
    bool Success,
    decimal RefundedAmount,
    string? ErrorMessage);

public record InstallmentDetail(int Count, decimal TotalPrice, decimal InstallmentPrice);

public record InstallmentInfoResult(bool Success, List<InstallmentDetail> Installments, string? ErrorMessage);

public interface IPaymentService
{
    Task<PaymentInitResult> InitiatePaymentAsync(InitiatePaymentDto dto);
    Task<PaymentResult> Handle3DSecureCallbackAsync(string conversationId, string token);
    Task<PaymentResult> ProcessDirectPaymentAsync(InitiatePaymentDto dto);
    Task<RefundResult> RefundPaymentAsync(int orderId, decimal? amount = null);
    Task<PaymentTransaction?> GetTransactionByOrderIdAsync(int orderId);
    Task<InstallmentInfoResult> GetInstallmentInfoAsync(string binNumber, decimal price);
}
