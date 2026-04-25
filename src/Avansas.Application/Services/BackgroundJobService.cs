using Avansas.Application.Interfaces;
using Avansas.Domain.Entities;
using Avansas.Domain.Enums;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avansas.Application.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<BackgroundJobService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendAbandonedCartEmailsAsync()
    {
        var threshold = DateTime.UtcNow.AddHours(-2);

        var abandonedCarts = await _unitOfWork.Carts.Query()
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .Include(c => c.User)
            .Where(c => c.UserId != null
                        && c.AbandonedEmailSentAt == null
                        && c.UpdatedAt < threshold)
            .ToListAsync();

        var cartsWithItems = abandonedCarts.Where(c => c.Items.Any()).ToList();

        _logger.LogInformation("Terk edilen sepet email görevi: {Count} sepet işlenecek", cartsWithItems.Count);

        foreach (var cart in cartsWithItems)
        {
            try
            {
                var cartUrl = "https://avansas.com/sepet";
                await _emailService.SendAbandonedCartEmailAsync(
                    cart.User!.Email!,
                    cart.User.FullName,
                    cartUrl);

                cart.AbandonedEmailSentAt = DateTime.UtcNow;
                _unitOfWork.Carts.Update(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Terk edilen sepet emaili gönderilemedi: UserId={UserId}", cart.UserId);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Terk edilen sepet email görevi tamamlandı");
    }

    public async Task ProcessStockNotificationsAsync()
    {
        var pendingNotifications = await _unitOfWork.StockNotifications.Query()
            .Include(s => s.Product)
            .Where(s => !s.IsNotified && s.Product.StockQuantity > 0)
            .ToListAsync();

        _logger.LogInformation("Stok bildirimi görevi: {Count} bildirim işlenecek", pendingNotifications.Count);

        foreach (var notification in pendingNotifications)
        {
            try
            {
                var productUrl = $"https://avansas.com/urun/{notification.Product.Slug}";
                await _emailService.SendStockBackInStockAsync(
                    notification.Email,
                    notification.Product.Name,
                    productUrl);

                notification.IsNotified = true;
                notification.NotifiedAt = DateTime.UtcNow;
                _unitOfWork.StockNotifications.Update(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok bildirimi gönderilemedi: NotificationId={Id}", notification.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Stok bildirimi görevi tamamlandı");
    }

    public async Task ProcessScheduledPriceRulesAsync()
    {
        var now = DateTime.UtcNow;

        // Başlangıç tarihi geçmiş ama henüz aktif olmayan kuralları aktif et
        var toActivate = await _unitOfWork.PriceRules.Query()
            .Where(p => !p.IsActive
                        && p.StartDate.HasValue
                        && p.StartDate <= now
                        && (!p.EndDate.HasValue || p.EndDate > now))
            .ToListAsync();

        foreach (var rule in toActivate)
        {
            rule.IsActive = true;
            _unitOfWork.PriceRules.Update(rule);
            _logger.LogInformation("Fiyat kuralı aktif edildi: {Name}", rule.Name);
        }

        // Bitiş tarihi geçmiş aktif kuralları pasif et
        var toDeactivate = await _unitOfWork.PriceRules.Query()
            .Where(p => p.IsActive
                        && p.EndDate.HasValue
                        && p.EndDate <= now)
            .ToListAsync();

        foreach (var rule in toDeactivate)
        {
            rule.IsActive = false;
            _unitOfWork.PriceRules.Update(rule);
            _logger.LogInformation("Fiyat kuralı pasif edildi: {Name}", rule.Name);
        }

        if (toActivate.Count > 0 || toDeactivate.Count > 0)
            await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Fiyat kuralı görevi tamamlandı: {Activated} aktif, {Deactivated} pasif",
            toActivate.Count, toDeactivate.Count);
    }

    public async Task CleanupExpiredDataAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        // 30 günden eski misafir sepetleri soft-delete et
        var oldGuestCarts = await _unitOfWork.Carts.Query()
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .Where(c => c.UserId == null && c.UpdatedAt < cutoff)
            .ToListAsync();

        foreach (var cart in oldGuestCarts)
        {
            foreach (var item in cart.Items)
                _unitOfWork.CartItems.SoftDelete(item);
            _unitOfWork.Carts.SoftDelete(cart);
        }

        if (oldGuestCarts.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Temizleme görevi: {Count} eski misafir sepeti silindi", oldGuestCarts.Count);
        }

        _logger.LogInformation("Temizleme görevi tamamlandı");
    }

    public async Task SendPaymentReminderEmailsAsync()
    {
        // 24-72 saat önce oluşturulmuş, havale bekleyen, ödenmemiş siparişler
        var from = DateTime.UtcNow.AddHours(-72);
        var to = DateTime.UtcNow.AddHours(-24);

        var pendingOrders = await _unitOfWork.Orders.Query()
            .Include(o => o.User)
            .Where(o => o.PaymentMethod == PaymentMethod.BankTransfer
                        && o.PaymentStatus == PaymentStatus.Pending
                        && o.CreatedAt >= from
                        && o.CreatedAt <= to)
            .ToListAsync();

        _logger.LogInformation("Ödeme hatırlatma görevi: {Count} sipariş işlenecek", pendingOrders.Count);

        foreach (var order in pendingOrders)
        {
            try
            {
                await _emailService.SendPaymentReminderAsync(
                    order.User.Email!,
                    order.User.FullName,
                    order.OrderNumber,
                    order.Total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme hatırlatma emaili gönderilemedi: OrderId={Id}", order.Id);
            }
        }

        _logger.LogInformation("Ödeme hatırlatma görevi tamamlandı");
    }
}
