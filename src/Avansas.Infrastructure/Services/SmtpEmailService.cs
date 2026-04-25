using System.Net;
using System.Net.Mail;
using System.Text;
using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Avansas.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _from;
    private readonly string _fromName;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
        _from = _config["Email:From"] ?? "noreply@avansas.com";
        _fromName = _config["Email:FromName"] ?? "Avansas";
    }

    public async Task SendOrderConfirmationAsync(OrderDto order)
    {
        var subject = $"Siparişiniz Alındı - #{order.OrderNumber}";
        var body = BuildOrderConfirmationBody(order);
        await SendAsync(order.UserEmail, order.UserFullName, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
    {
        var subject = "Avansas'a Hoş Geldiniz!";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#007bff;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;'>avansas</h1>
  </div>
  <div style='padding:32px;background:#fff;'>
    <h2>Merhaba {fullName},</h2>
    <p>Avansas ailesine hoş geldiniz! Hesabınız başarıyla oluşturuldu.</p>
    <p>Binlerce ürün arasından alışveriş yapabilir, siparişlerinizi takip edebilirsiniz.</p>
    <div style='text-align:center;margin:32px 0;'>
      <a href='https://avansas.com/urunler' style='background:#007bff;color:#fff;padding:12px 32px;text-decoration:none;border-radius:4px;'>Alışverişe Başla</a>
    </div>
  </div>
  <div style='padding:16px;text-align:center;color:#999;font-size:12px;background:#f8f9fa;'>
    &copy; {DateTime.Now.Year} Avansas. Tüm hakları saklıdır.
  </div>
</div>";
        await SendAsync(toEmail, fullName, subject, body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string fullName, string resetLink)
    {
        var subject = "Şifre Sıfırlama Talebi";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#007bff;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;'>avansas</h1>
  </div>
  <div style='padding:32px;background:#fff;'>
    <h2>Merhaba {fullName},</h2>
    <p>Şifre sıfırlama talebinde bulundunuz. Aşağıdaki butona tıklayarak şifrenizi sıfırlayabilirsiniz.</p>
    <p style='color:#dc3545;'>Bu link 24 saat geçerlidir. Talebi siz yapmadıysanız bu e-postayı görmezden gelin.</p>
    <div style='text-align:center;margin:32px 0;'>
      <a href='{resetLink}' style='background:#dc3545;color:#fff;padding:12px 32px;text-decoration:none;border-radius:4px;'>Şifremi Sıfırla</a>
    </div>
  </div>
  <div style='padding:16px;text-align:center;color:#999;font-size:12px;background:#f8f9fa;'>
    &copy; {DateTime.Now.Year} Avansas. Tüm hakları saklıdır.
  </div>
</div>";
        await SendAsync(toEmail, fullName, subject, body);
    }

    public async Task SendOrderStatusUpdateAsync(OrderDto order)
    {
        var subject = $"Siparişiniz Güncellendi - #{order.OrderNumber}";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#007bff;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;'>avansas</h1>
  </div>
  <div style='padding:32px;background:#fff;'>
    <h2>Merhaba {order.UserFullName},</h2>
    <p>#{order.OrderNumber} numaralı siparişinizin durumu güncellendi.</p>
    <p style='font-size:18px;'><strong>Yeni Durum:</strong> <span style='color:#007bff;'>{order.StatusText}</span></p>
    {(string.IsNullOrEmpty(order.CargoTrackingNumber) ? "" : $"<p><strong>Kargo Takip No:</strong> {order.CargoTrackingNumber} ({order.CargoCompany})</p>")}
    <div style='text-align:center;margin:32px 0;'>
      <a href='https://avansas.com/hesap/siparis/{order.Id}' style='background:#007bff;color:#fff;padding:12px 32px;text-decoration:none;border-radius:4px;'>Siparişi Görüntüle</a>
    </div>
  </div>
  <div style='padding:16px;text-align:center;color:#999;font-size:12px;background:#f8f9fa;'>
    &copy; {DateTime.Now.Year} Avansas. Tüm hakları saklıdır.
  </div>
</div>";
        await SendAsync(order.UserEmail, order.UserFullName, subject, body);
    }

    public async Task SendNewOrderNotificationToAdminAsync(OrderDto order)
    {
        var adminEmail = _config["Email:AdminEmail"] ?? "admin@avansas.com";
        var subject = $"Yeni Sipariş! #{order.OrderNumber} - {order.Total:N2} ₺";

        var itemsHtml = new StringBuilder();
        foreach (var item in order.Items)
        {
            itemsHtml.Append($@"
<tr>
  <td style='padding:8px;border-bottom:1px solid #eee;'>{item.ProductName}</td>
  <td style='padding:8px;border-bottom:1px solid #eee;text-align:center;'>{item.Quantity}</td>
  <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>{item.UnitPrice:N2} ₺</td>
  <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>{item.TotalPrice:N2} ₺</td>
</tr>");
        }

        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#dc3545;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;'>Yeni Sipariş Bildirimi</h1>
  </div>
  <div style='padding:32px;background:#fff;'>
    <div style='background:#f8f9fa;border-radius:8px;padding:16px;margin-bottom:24px;'>
      <table style='width:100%;'>
        <tr><td style='padding:4px 0;color:#666;'>Sipariş No:</td><td style='font-weight:bold;'>#{order.OrderNumber}</td></tr>
        <tr><td style='padding:4px 0;color:#666;'>Tarih:</td><td>{order.CreatedAt:dd.MM.yyyy HH:mm}</td></tr>
        <tr><td style='padding:4px 0;color:#666;'>Müşteri:</td><td>{order.UserFullName}</td></tr>
        <tr><td style='padding:4px 0;color:#666;'>E-posta:</td><td>{order.UserEmail}</td></tr>
        <tr><td style='padding:4px 0;color:#666;'>Ödeme:</td><td>{order.PaymentMethodText}</td></tr>
        <tr><td style='padding:4px 0;color:#666;'>Durum:</td><td><span style='background:#ffc107;color:#000;padding:2px 8px;border-radius:4px;font-size:12px;'>{order.StatusText}</span></td></tr>
      </table>
    </div>
    <h3 style='margin-bottom:12px;'>Sipariş Kalemleri</h3>
    <table style='width:100%;border-collapse:collapse;'>
      <thead>
        <tr style='background:#f8f9fa;'>
          <th style='padding:8px;text-align:left;'>Ürün</th>
          <th style='padding:8px;text-align:center;'>Adet</th>
          <th style='padding:8px;text-align:right;'>Birim</th>
          <th style='padding:8px;text-align:right;'>Toplam</th>
        </tr>
      </thead>
      <tbody>{itemsHtml}</tbody>
    </table>
    <div style='margin-top:16px;padding:12px;background:#f8f9fa;border-radius:8px;'>
      <table style='width:100%;'>
        <tr><td>Ara Toplam</td><td style='text-align:right;'>{order.SubTotal:N2} ₺</td></tr>
        {(order.DiscountAmount > 0 ? $"<tr><td style='color:#28a745;'>İndirim ({order.CouponCode})</td><td style='text-align:right;color:#28a745;'>-{order.DiscountAmount:N2} ₺</td></tr>" : "")}
        <tr><td>Kargo</td><td style='text-align:right;'>{(order.ShippingCost == 0 ? "Ücretsiz" : $"{order.ShippingCost:N2} ₺")}</td></tr>
        <tr style='font-weight:bold;font-size:18px;border-top:2px solid #dee2e6;'>
          <td style='padding-top:8px;'>TOPLAM</td>
          <td style='padding-top:8px;text-align:right;color:#dc3545;'>{order.Total:N2} ₺</td>
        </tr>
      </table>
    </div>
    <h3 style='margin-top:24px;margin-bottom:12px;'>Teslimat Bilgileri</h3>
    <div style='background:#f8f9fa;border-radius:8px;padding:12px;'>
      <p style='margin:4px 0;'><strong>{order.ShippingFirstName} {order.ShippingLastName}</strong></p>
      <p style='margin:4px 0;'>{order.ShippingAddress}</p>
      <p style='margin:4px 0;'>{order.ShippingDistrict} / {order.ShippingCity}</p>
      <p style='margin:4px 0;'>Tel: {order.ShippingPhone}</p>
    </div>
    {(string.IsNullOrEmpty(order.Note) ? "" : $"<div style='margin-top:16px;padding:12px;background:#fff3cd;border-radius:8px;'><strong>Müşteri Notu:</strong> {order.Note}</div>")}
    <div style='text-align:center;margin:32px 0;'>
      <a href='https://avansas.com/admin/orders/detail/{order.Id}' style='background:#dc3545;color:#fff;padding:12px 32px;text-decoration:none;border-radius:4px;font-weight:bold;'>Siparişi Yönet</a>
    </div>
  </div>
  <div style='padding:16px;text-align:center;color:#999;font-size:12px;background:#f8f9fa;'>
    Bu otomatik bir bildirimdir. &copy; {DateTime.Now.Year} Avansas Admin
  </div>
</div>";
        await SendAsync(adminEmail, "Avansas Admin", subject, body);
    }

    public async Task SendBulkOrderRequestAsync(string companyName, string contactName, string email, string phone, string message)
    {
        var adminEmail = _config["Email:AdminEmail"] ?? "admin@avansas.com";
        var subject = $"Yeni Toplu Sipariş Talebi - {companyName}";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#28a745;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;'>Toplu Sipariş Talebi</h1>
  </div>
  <div style='padding:32px;background:#fff;'>
    <div style='background:#f8f9fa;border-radius:8px;padding:16px;margin-bottom:24px;'>
      <table style='width:100%;'>
        <tr><td style='padding:4px 0;color:#666;'>Firma Adı:</td><td style='font-weight:bold;'>{companyName}</td></tr>
        <tr><td style='padding:4px 0;color:#666;'>Yetkili:</td><td>{contactName}</td></tr>
        <tr><td style='padding:4px 0;color:#666;'>E-posta:</td><td>{email}</td></tr>
        <tr><td style='padding:4px 0;color:#666;'>Telefon:</td><td>{phone}</td></tr>
      </table>
    </div>
    <h3 style='margin-bottom:12px;'>Talep Detayı</h3>
    <div style='background:#f8f9fa;border-radius:8px;padding:16px;white-space:pre-line;'>{message}</div>
  </div>
  <div style='padding:16px;text-align:center;color:#999;font-size:12px;background:#f8f9fa;'>
    Bu otomatik bir bildirimdir. &copy; {DateTime.Now.Year} Avansas
  </div>
</div>";
        await SendAsync(adminEmail, "Avansas Admin", subject, body);
    }

    public async Task SendAbandonedCartEmailAsync(string toEmail, string fullName, string cartUrl)
    {
        var subject = "Sepetinizde ürünler bekleniyor!";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#007bff;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;'>avansas</h1>
  </div>
  <div style='padding:32px;background:#fff;'>
    <h2>Merhaba {fullName},</h2>
    <p>Sepetinizde ürünler bıraktınız. Alışverişinizi tamamlamayı unutmayın!</p>
    <div style='text-align:center;margin:32px 0;'>
      <a href='{cartUrl}' style='background:#007bff;color:#fff;padding:12px 32px;text-decoration:none;border-radius:4px;font-weight:bold;'>Sepetime Git</a>
    </div>
    <p style='color:#999;font-size:12px;'>Sepetinizdeki ürünler stok değişikliklerine bağlı olarak güncellenebilir.</p>
  </div>
  <div style='padding:16px;text-align:center;color:#999;font-size:12px;background:#f8f9fa;'>
    &copy; {DateTime.Now.Year} Avansas. Tüm hakları saklıdır.
  </div>
</div>";
        await SendAsync(toEmail, fullName, subject, body);
    }

    public async Task SendStockBackInStockAsync(string toEmail, string productName, string productUrl)
    {
        var subject = $"{productName} Tekrar Stokta!";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#28a745;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;'>avansas</h1>
  </div>
  <div style='padding:32px;background:#fff;'>
    <h2>Müjde!</h2>
    <p>Stok bildirimi talebinde bulunduğunuz <strong>{productName}</strong> tekrar stoka girdi.</p>
    <p>Hızlı davranın, stoklar sınırlıdır!</p>
    <div style='text-align:center;margin:32px 0;'>
      <a href='{productUrl}' style='background:#28a745;color:#fff;padding:12px 32px;text-decoration:none;border-radius:4px;font-weight:bold;'>Ürünü İncele</a>
    </div>
  </div>
  <div style='padding:16px;text-align:center;color:#999;font-size:12px;background:#f8f9fa;'>
    &copy; {DateTime.Now.Year} Avansas. Tüm hakları saklıdır.
  </div>
</div>";
        await SendAsync(toEmail, productName, subject, body);
    }

    public async Task SendPaymentReminderAsync(string toEmail, string fullName, string orderNumber, decimal total)
    {
        var subject = $"Ödemeniz Bekleniyor - #{orderNumber}";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#ffc107;padding:24px;text-align:center;'>
    <h1 style='color:#212529;margin:0;'>avansas</h1>
  </div>
  <div style='padding:32px;background:#fff;'>
    <h2>Merhaba {fullName},</h2>
    <p>#{orderNumber} numaralı siparişiniz için havale/EFT ödemesi beklenmektedir.</p>
    <div style='background:#fff3cd;border:1px solid #ffc107;border-radius:8px;padding:16px;margin:16px 0;'>
      <strong>Sipariş No:</strong> #{orderNumber}<br/>
      <strong>Toplam Tutar:</strong> {total:N2} ₺
    </div>
    <p>48 saat içinde ödeme yapılmaması durumunda siparişiniz iptal edilecektir.</p>
    <div style='text-align:center;margin:32px 0;'>
      <a href='https://avansas.com/hesap/siparis/{orderNumber}' style='background:#ffc107;color:#212529;padding:12px 32px;text-decoration:none;border-radius:4px;font-weight:bold;'>Sipariş Detayı</a>
    </div>
  </div>
  <div style='padding:16px;text-align:center;color:#999;font-size:12px;background:#f8f9fa;'>
    &copy; {DateTime.Now.Year} Avansas. Tüm hakları saklıdır.
  </div>
</div>";
        await SendAsync(toEmail, fullName, subject, body);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string body)
    {
        try
        {
            var host = _config["Email:Smtp:Host"];
            if (string.IsNullOrEmpty(host))
            {
                _logger.LogWarning("SMTP ayarları yapılandırılmamış. E-posta gönderilmedi: {Subject}", subject);
                return;
            }

            var port = int.Parse(_config["Email:Smtp:Port"] ?? "587");
            var user = _config["Email:Smtp:Username"] ?? "";
            var pass = _config["Email:Smtp:Password"] ?? "";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass)
            };

            var message = new MailMessage
            {
                From = new MailAddress(_from, _fromName, Encoding.UTF8),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(message);
            _logger.LogInformation("E-posta gönderildi: {To} - {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta gönderilemedi: {To} - {Subject}", toEmail, subject);
        }
    }

    private string BuildOrderConfirmationBody(OrderDto order)
    {
        var itemsHtml = new StringBuilder();
        foreach (var item in order.Items)
        {
            itemsHtml.Append($@"
<tr>
  <td style='padding:8px;border-bottom:1px solid #eee;'>{item.ProductName}</td>
  <td style='padding:8px;border-bottom:1px solid #eee;text-align:center;'>{item.Quantity}</td>
  <td style='padding:8px;border-bottom:1px solid #eee;text-align:right;'>{item.TotalPrice:N2} ₺</td>
</tr>");
        }

        return $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'>
  <div style='background:#007bff;padding:24px;text-align:center;'>
    <h1 style='color:#fff;margin:0;'>avansas</h1>
  </div>
  <div style='padding:32px;background:#fff;'>
    <h2>Siparişiniz Alındı!</h2>
    <p>Merhaba {order.UserFullName}, siparişiniz başarıyla alındı.</p>
    <p><strong>Sipariş No:</strong> #{order.OrderNumber}</p>
    <p><strong>Tarih:</strong> {order.CreatedAt:dd.MM.yyyy HH:mm}</p>
    <table style='width:100%;border-collapse:collapse;margin:16px 0;'>
      <thead>
        <tr style='background:#f8f9fa;'>
          <th style='padding:8px;text-align:left;'>Ürün</th>
          <th style='padding:8px;text-align:center;'>Adet</th>
          <th style='padding:8px;text-align:right;'>Tutar</th>
        </tr>
      </thead>
      <tbody>{itemsHtml}</tbody>
    </table>
    <table style='width:100%;margin-top:16px;'>
      <tr><td>Ara Toplam</td><td style='text-align:right;'>{order.SubTotal:N2} ₺</td></tr>
      {(order.DiscountAmount > 0 ? $"<tr><td style='color:#28a745;'>İndirim</td><td style='text-align:right;color:#28a745;'>-{order.DiscountAmount:N2} ₺</td></tr>" : "")}
      <tr><td>Kargo</td><td style='text-align:right;'>{(order.ShippingCost == 0 ? "Ücretsiz" : $"{order.ShippingCost:N2} ₺")}</td></tr>
      <tr style='font-weight:bold;font-size:16px;border-top:2px solid #eee;'>
        <td style='padding-top:8px;'>Toplam</td>
        <td style='padding-top:8px;text-align:right;'>{order.Total:N2} ₺</td>
      </tr>
    </table>
    <div style='text-align:center;margin:32px 0;'>
      <a href='https://avansas.com/hesap/siparis/{order.Id}' style='background:#007bff;color:#fff;padding:12px 32px;text-decoration:none;border-radius:4px;'>Siparişi Takip Et</a>
    </div>
  </div>
  <div style='padding:16px;text-align:center;color:#999;font-size:12px;background:#f8f9fa;'>
    &copy; {DateTime.Now.Year} Avansas. Tüm hakları saklıdır.
  </div>
</div>";
    }
}
