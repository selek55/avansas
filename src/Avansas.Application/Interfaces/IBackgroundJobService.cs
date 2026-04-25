namespace Avansas.Application.Interfaces;

public interface IBackgroundJobService
{
    /// <summary>
    /// UserId dolu, öğeli, 2+ saat önce güncellenen ve email gönderilmemiş sepetlere hatırlatma emaili gönderir.
    /// </summary>
    Task SendAbandonedCartEmailsAsync();

    /// <summary>
    /// Stoğa dönen ürünler için stok bildirimi abonelerine email gönderir.
    /// </summary>
    Task ProcessStockNotificationsAsync();

    /// <summary>
    /// Başlangıç/bitiş tarihine göre fiyat kurallarını aktif/pasif yapar.
    /// </summary>
    Task ProcessScheduledPriceRulesAsync();

    /// <summary>
    /// 30 günden eski misafir sepetleri ve süresi dolmuş verileri temizler.
    /// </summary>
    Task CleanupExpiredDataAsync();

    /// <summary>
    /// Havale bekleyen siparişler için 48 saat sonra hatırlatma emaili gönderir.
    /// </summary>
    Task SendPaymentReminderEmailsAsync();
}
