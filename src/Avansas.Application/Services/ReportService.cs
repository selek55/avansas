using Avansas.Application.DTOs;
using Avansas.Application.Interfaces;
using Avansas.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Avansas.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ReportDto> GetDashboardReportAsync(int days = 30)
    {
        var topProducts = await GetTopProductsAsync(10, days);
        var dailyRevenue = await GetDailyRevenueAsync(days);
        var categorySales = await GetCategorySalesAsync(days);

        var since = DateTime.UtcNow.AddDays(-days);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var orders = _uow.Orders.Query().Where(o => o.CreatedAt >= since);
        var totalCustomers = await orders.Select(o => o.UserId).Distinct().CountAsync();
        var newCustomers = await _uow.Orders.Query()
            .Where(o => o.CreatedAt >= monthStart)
            .Select(o => o.UserId).Distinct().CountAsync();
        var orderCount = await orders.CountAsync();
        var totalRevenue = orderCount > 0 ? await orders.SumAsync(o => o.Total) : 0;
        var avgOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0;

        return new ReportDto
        {
            TopProducts = topProducts,
            DailyRevenue = dailyRevenue,
            CategorySales = categorySales,
            TotalCustomers = totalCustomers,
            NewCustomersThisMonth = newCustomers,
            AverageOrderValue = avgOrderValue
        };
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync(int count = 10, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        return await _uow.OrderItems.Query()
            .Where(oi => oi.Order.CreatedAt >= since)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                TotalSold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<DailyRevenueDto>> GetDailyRevenueAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        return await _uow.Orders.Query()
            .Where(o => o.CreatedAt >= since)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Revenue = g.Sum(o => o.Total),
                OrderCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<List<CategorySalesDto>> GetCategorySalesAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        return await _uow.OrderItems.Query()
            .Where(oi => oi.Order.CreatedAt >= since)
            .GroupBy(oi => oi.Product.Category!.Name)
            .Select(g => new CategorySalesDto
            {
                CategoryName = g.Key ?? "Kategorisiz",
                TotalSold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToListAsync();
    }
}
