using ReUse.Application.DTOs.Analytics;
using ReUse.Application.Enums;
using ReUse.Application.Extensions;
using ReUse.Application.Interfaces.Repository;
using ReUse.Application.Interfaces.Services;

namespace ReUse.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;

    public AnalyticsService(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<DashboardResponse> GetDashboardAsync(DashboardPeriod period, int productPage = 0, int productPageSize = 10, int sellerPage = 0, int sellerPageSize = 10)
    {
        if (!Enum.IsDefined(typeof(DashboardPeriod), period))
            throw new InvalidOperationException("Invalid period value.");

        var window = period.ToWindow(DateTime.UtcNow);

        var summary = await _analyticsRepository.GetSummaryAsync(window.CurrentStart, window.CurrentEnd);
        var revenueTrend = await _analyticsRepository.GetRevenueTrendAsync(window.CurrentStart, window.CurrentEnd);
        var orderVolume = await _analyticsRepository.GetOrderVolumeAsync(window.CurrentStart, window.CurrentEnd);
        var salesByCategory = await _analyticsRepository.GetSalesByCategoryAsync(window.CurrentStart, window.CurrentEnd);
        var userActivity = await _analyticsRepository.GetUserActivityAsync(window.CurrentStart, window.CurrentEnd);
        var productPerformance = await _analyticsRepository.GetProductPerformanceAsync(window.CurrentStart, window.CurrentEnd);
        var topSellers = await _analyticsRepository.GetTopSellersAsync(window.CurrentStart, window.CurrentEnd);

        return new DashboardResponse
        {
            Period = period,
            CurrentPeriodStart = window.CurrentStart,
            CurrentPeriodEnd = window.CurrentEnd,
            Summary = summary,
            RevenueTrend = revenueTrend,
            OrderVolume = orderVolume,
            SalesByCategory = salesByCategory,
            UserActivity = userActivity,
            ProductPerformance = PaginatedResult<ProductPerformanceRow>.Create(productPerformance, productPage, productPageSize),
            TopSellers = PaginatedResult<TopSellerRow>.Create(topSellers, sellerPage, sellerPageSize),
        };
    }
}