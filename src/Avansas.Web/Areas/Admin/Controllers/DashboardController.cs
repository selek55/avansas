using Avansas.Application.Interfaces;
using Avansas.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class DashboardController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IUserService _userService;

    public DashboardController(IOrderService orderService, IProductService productService, IUserService userService)
    {
        _orderService = orderService;
        _productService = productService;
        _userService = userService;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _orderService.GetOrderStatisticsAsync();
        var recentOrders = await _orderService.GetOrdersAsync(1, 10);
        var lowStockProducts = await _productService.GetProductsAsync(new ProductFilterDto
        {
            InStock = false, PageSize = 10, IsActive = true
        });

        ViewBag.Stats = stats;
        ViewBag.RecentOrders = recentOrders.Items;
        ViewBag.LowStockProducts = lowStockProducts.Items;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ChartData(int days = 30)
    {
        var orders = await _orderService.GetOrdersAsync(1, 1000);
        var startDate = DateTime.UtcNow.AddDays(-days);
        var filtered = orders.Items.Where(o => o.CreatedAt >= startDate).ToList();

        var dailyData = Enumerable.Range(0, days).Select(i =>
        {
            var date = DateTime.UtcNow.Date.AddDays(-days + 1 + i);
            var dayOrders = filtered.Where(o => o.CreatedAt.Date == date).ToList();
            return new { date = date.ToString("dd.MM"), revenue = dayOrders.Sum(o => o.Total), count = dayOrders.Count };
        }).ToList();

        var statusData = filtered.GroupBy(o => o.StatusText)
            .Select(g => new { status = g.Key, count = g.Count() }).ToList();

        return Json(new { daily = dailyData, statusBreakdown = statusData });
    }
}
