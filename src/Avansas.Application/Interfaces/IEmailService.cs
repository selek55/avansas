using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(OrderDto order);
    Task SendWelcomeEmailAsync(string toEmail, string fullName);
    Task SendPasswordResetAsync(string toEmail, string fullName, string resetLink);
    Task SendOrderStatusUpdateAsync(OrderDto order);
    Task SendNewOrderNotificationToAdminAsync(OrderDto order);
    Task SendBulkOrderRequestAsync(string companyName, string contactName, string email, string phone, string message);

    // Background job emails
    Task SendAbandonedCartEmailAsync(string toEmail, string fullName, string cartUrl);
    Task SendStockBackInStockAsync(string toEmail, string productName, string productUrl);
    Task SendPaymentReminderAsync(string toEmail, string fullName, string orderNumber, decimal total);
}
