using Avansas.Application.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(int days = 30)
    {
        var report = await _reportService.GetDashboardReportAsync(days);
        ViewBag.Days = days;
        return View(report);
    }

    public async Task<IActionResult> TopProducts(int days = 30)
    {
        var products = await _reportService.GetTopProductsAsync(20, days);
        ViewBag.Days = days;
        return View(products);
    }

    public async Task<IActionResult> Revenue(int days = 30)
    {
        var revenue = await _reportService.GetDailyRevenueAsync(days);
        ViewBag.Days = days;
        return View(revenue);
    }

    public async Task<IActionResult> ExportReport(int days = 30)
    {
        var report = await _reportService.GetDashboardReportAsync(days);

        using var workbook = new XLWorkbook();

        // Top Products sheet
        var wsProducts = workbook.Worksheets.Add("En Cok Satanlar");
        wsProducts.Cell(1, 1).Value = "Urun Adi";
        wsProducts.Cell(1, 2).Value = "Satilan Adet";
        wsProducts.Cell(1, 3).Value = "Toplam Gelir";
        wsProducts.Row(1).Style.Font.Bold = true;
        for (int i = 0; i < report.TopProducts.Count; i++)
        {
            wsProducts.Cell(i + 2, 1).Value = report.TopProducts[i].ProductName;
            wsProducts.Cell(i + 2, 2).Value = report.TopProducts[i].TotalSold;
            wsProducts.Cell(i + 2, 3).Value = report.TopProducts[i].TotalRevenue;
        }
        wsProducts.Columns().AdjustToContents();

        // Daily Revenue sheet
        var wsRevenue = workbook.Worksheets.Add("Gunluk Gelir");
        wsRevenue.Cell(1, 1).Value = "Tarih";
        wsRevenue.Cell(1, 2).Value = "Gelir";
        wsRevenue.Cell(1, 3).Value = "Siparis Sayisi";
        wsRevenue.Row(1).Style.Font.Bold = true;
        for (int i = 0; i < report.DailyRevenue.Count; i++)
        {
            wsRevenue.Cell(i + 2, 1).Value = report.DailyRevenue[i].Date;
            wsRevenue.Cell(i + 2, 2).Value = report.DailyRevenue[i].Revenue;
            wsRevenue.Cell(i + 2, 3).Value = report.DailyRevenue[i].OrderCount;
        }
        wsRevenue.Columns().AdjustToContents();

        // Category Sales sheet
        var wsCategory = workbook.Worksheets.Add("Kategori Satislari");
        wsCategory.Cell(1, 1).Value = "Kategori";
        wsCategory.Cell(1, 2).Value = "Satilan Adet";
        wsCategory.Cell(1, 3).Value = "Toplam Gelir";
        wsCategory.Row(1).Style.Font.Bold = true;
        for (int i = 0; i < report.CategorySales.Count; i++)
        {
            wsCategory.Cell(i + 2, 1).Value = report.CategorySales[i].CategoryName;
            wsCategory.Cell(i + 2, 2).Value = report.CategorySales[i].TotalSold;
            wsCategory.Cell(i + 2, 3).Value = report.CategorySales[i].TotalRevenue;
        }
        wsCategory.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"rapor_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
