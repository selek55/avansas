using Avansas.Application.DTOs;

namespace Avansas.Application.Interfaces;

public interface IReportService
{
    Task<ReportDto> GetDashboardReportAsync(int days = 30);
    Task<List<TopProductDto>> GetTopProductsAsync(int count = 10, int days = 30);
    Task<List<DailyRevenueDto>> GetDailyRevenueAsync(int days = 30);
    Task<List<CategorySalesDto>> GetCategorySalesAsync(int days = 30);
}
