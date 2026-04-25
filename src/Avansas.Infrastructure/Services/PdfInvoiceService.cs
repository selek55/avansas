using Avansas.Application.Interfaces;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Avansas.Infrastructure.Services;

public class PdfInvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;

    public PdfInvoiceService(IUnitOfWork unitOfWork, IConfiguration config)
    {
        _unitOfWork = unitOfWork;
        _config = config;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string GetInvoiceNumber(int orderId) => $"FTR-{DateTime.Now.Year}-{orderId:D6}";

    public async Task<byte[]> GenerateOrderInvoicePdfAsync(int orderId)
    {
        var order = await _unitOfWork.Orders.Query()
            .Include(o => o.Items)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException("Sipariş bulunamadı.");

        var invoiceNo = GetInvoiceNumber(orderId);
        var companyName = _config["Company:Name"] ?? "Avansas";
        var companyAddress = _config["Company:Address"] ?? "İstanbul, Türkiye";
        var companyTaxNo = _config["Company:TaxNumber"] ?? "-";
        var companyTaxOffice = _config["Company:TaxOffice"] ?? "-";
        var companyPhone = _config["Company:Phone"] ?? "-";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(companyName).Bold().FontSize(18);
                        col.Item().Text(companyAddress).FontSize(9);
                        col.Item().Text($"Vergi No: {companyTaxNo} / {companyTaxOffice}").FontSize(9);
                        col.Item().Text($"Tel: {companyPhone}").FontSize(9);
                    });
                    row.ConstantItem(150).Column(col =>
                    {
                        col.Item().Background(Colors.Blue.Lighten3).Padding(8).Column(c =>
                        {
                            c.Item().Text("FATURA").Bold().FontSize(14).AlignCenter();
                            c.Item().Text(invoiceNo).FontSize(9).AlignCenter();
                            c.Item().Text($"Tarih: {DateTime.Now:dd.MM.yyyy}").FontSize(9).AlignCenter();
                            c.Item().Text($"Sipariş: #{order.OrderNumber}").FontSize(9).AlignCenter();
                        });
                    });
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Border(1).Padding(8).Column(c =>
                        {
                            c.Item().Text("Satıcı Bilgileri").Bold();
                            c.Item().Text(companyName);
                            c.Item().Text(companyAddress);
                        });
                        row.ConstantItem(20);
                        row.RelativeItem().Border(1).Padding(8).Column(c =>
                        {
                            c.Item().Text("Müşteri Bilgileri").Bold();
                            c.Item().Text($"{order.BillingFirstName} {order.BillingLastName}");
                            if (!string.IsNullOrEmpty(order.BillingCompanyName))
                                c.Item().Text(order.BillingCompanyName);
                            if (!string.IsNullOrEmpty(order.BillingTaxNumber))
                                c.Item().Text($"VKN: {order.BillingTaxNumber} / {order.BillingTaxOffice}");
                            c.Item().Text(order.BillingAddress);
                            c.Item().Text(order.BillingCity);
                        });
                    });

                    col.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Ürün Adı").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Adet").Bold().AlignCenter();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Birim Fiyat").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("KDV %").Bold().AlignCenter();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text("Toplam").Bold().AlignRight();
                        });

                        foreach (var item in order.Items)
                        {
                            table.Cell().Padding(4).Text(item.ProductName);
                            table.Cell().Padding(4).Text(item.Quantity.ToString()).AlignCenter();
                            table.Cell().Padding(4).Text($"{item.UnitPrice:N2} ₺").AlignRight();
                            table.Cell().Padding(4).Text($"%{item.TaxRate:0}").AlignCenter();
                            table.Cell().Padding(4).Text($"{item.TotalPrice:N2} ₺").AlignRight();
                        }
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(200).Border(1).Padding(8).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Ara Toplam");
                                r.ConstantItem(80).Text($"{order.SubTotal:N2} ₺").AlignRight();
                            });
                            if (order.DiscountAmount > 0)
                            {
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("İndirim");
                                    r.ConstantItem(80).Text($"-{order.DiscountAmount:N2} ₺").AlignRight();
                                });
                            }
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("KDV");
                                r.ConstantItem(80).Text($"{order.TaxAmount:N2} ₺").AlignRight();
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Kargo");
                                r.ConstantItem(80).Text(order.ShippingCost == 0 ? "Ücretsiz" : $"{order.ShippingCost:N2} ₺").AlignRight();
                            });
                            c.Item().BorderTop(1).PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text("GENEL TOPLAM").Bold();
                                r.ConstantItem(80).Text($"{order.Total:N2} ₺").Bold().AlignRight();
                            });
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Bu belge elektronik olarak düzenlenmiştir. | ");
                    text.Span($"{companyName} © {DateTime.Now.Year}");
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateReturnInvoicePdfAsync(int returnRequestId)
    {
        var returnReq = await _unitOfWork.ReturnRequests.Query()
            .Include(r => r.Order)
            .Include(r => r.Items)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId)
            ?? throw new InvalidOperationException("İade talebi bulunamadı.");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Header().Text($"İADE FATURASI — #{returnReq.Order.OrderNumber}").Bold().FontSize(16);
                page.Content().Text($"İade ID: {returnRequestId} — Tutar: {returnReq.RefundAmount:N2} ₺").FontSize(12);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateProformaInvoicePdfAsync(int orderId)
    {
        // Proforma = normal fatura ile aynı ama "PROFORMA" başlıklı
        var pdf = await GenerateOrderInvoicePdfAsync(orderId);
        return pdf; // Gerçek uygulamada başlık farklı olur
    }
}
