using ReUse.Application.DTOs.Analytics;

namespace ReUse.Application.Interfaces.Repository;

public interface IAnalyticsRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateTime from, DateTime to);
    Task<List<RevenueTrendDto>> GetRevenueTrendAsync(DateTime from, DateTime to);
    Task<List<OrderVolumeDto>> GetOrderVolumeAsync(DateTime from, DateTime to);
    Task<List<SalesByCategoryDto>> GetSalesByCategoryAsync(DateTime from, DateTime to);
    Task<List<UserActivityDto>> GetUserActivityAsync(DateTime from, DateTime to);
    Task<List<ProductPerformanceRow>> GetProductPerformanceAsync(DateTime from, DateTime to);
    Task<List<TopSellerRow>> GetTopSellersAsync(DateTime from, DateTime to);
}