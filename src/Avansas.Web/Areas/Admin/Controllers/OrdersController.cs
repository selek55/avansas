using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IShipmentService _shipmentService;
    private readonly IInvoiceService _invoiceService;

    public OrdersController(IOrderService orderService, IShipmentService shipmentService, IInvoiceService invoiceService)
    {
        _orderService = orderService;
        _shipmentService = shipmentService;
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index(int page = 1, OrderStatus? status = null)
    {
        var result = await _orderService.GetOrdersAsync(page, 20, status);
        ViewBag.CurrentStatus = status;
        return View(result);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(UpdateOrderStatusDto dto)
    {
        await _orderService.UpdateOrderStatusAsync(dto);
        TempData["Success"] = "Sipariş durumu güncellendi";
        return RedirectToAction(nameof(Detail), new { id = dto.OrderId });
    }

    public async Task<IActionResult> ExportExcel(OrderStatus? status = null)
    {
        // Fetch all orders (large page size to get everything)
        var result = await _orderService.GetOrdersAsync(1, 10000, status);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Siparişler");

        // Header row
        var headers = new[] { "Sipariş No", "Müşteri", "E-posta", "Durum", "Ödeme Durumu",
            "Ara Toplam", "Kargo", "İndirim", "Vergi", "Toplam", "Şehir", "Tarih" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRow = ws.Range(1, 1, 1, headers.Length);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a1a2e");
        headerRow.Style.Font.FontColor = XLColor.White;

        // Data rows
        int row = 2;
        foreach (var order in result.Items)
        {
            ws.Cell(row, 1).Value = order.OrderNumber;
            ws.Cell(row, 2).Value = order.UserFullName;
            ws.Cell(row, 3).Value = order.UserEmail;
            ws.Cell(row, 4).Value = order.StatusText;
            ws.Cell(row, 5).Value = order.PaymentStatusText;
            ws.Cell(row, 6).Value = order.SubTotal;
            ws.Cell(row, 7).Value = order.ShippingCost;
            ws.Cell(row, 8).Value = order.DiscountAmount;
            ws.Cell(row, 9).Value = order.TaxAmount;
            ws.Cell(row, 10).Value = order.Total;
            ws.Cell(row, 11).Value = order.ShippingCity;
            ws.Cell(row, 12).Value = order.CreatedAt.ToString("dd.MM.yyyy HH:mm");
            row++;
        }

        // Format currency columns
        var currencyRange = ws.Range(2, 6, row - 1, 10);
        currencyRange.Style.NumberFormat.Format = "#,##0.00";

        // Auto-fit columns
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"Siparisler_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    public async Task<IActionResult> PrintInvoice(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [Route("admin/orders/{id}/create-shipment")]
    public async Task<IActionResult> CreateShipment(int id, int cargoCompany, string trackingNumber)
    {
        var dto = new CreateShipmentDto(id, (CargoCompany)cargoCompany, trackingNumber, null);
        await _shipmentService.CreateShipmentAsync(dto);
        TempData["Success"] = "Kargo bilgisi kaydedildi.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet]
    [Route("admin/orders/{id}/download-invoice")]
    public async Task<IActionResult> DownloadInvoice(int id)
    {
        var pdfBytes = await _invoiceService.GenerateOrderInvoicePdfAsync(id);
        var invoiceNumber = _invoiceService.GetInvoiceNumber(id);
        return File(pdfBytes, "application/pdf", $"{invoiceNumber}.pdf");
    }
}
