using ReUse.Application.Enums;

namespace ReUse.Application.DTOs.Analytics;

public record DashboardResponse
{
    public DashboardPeriod Period { get; init; }
    public DateTime CurrentPeriodStart { get; init; }
    public DateTime CurrentPeriodEnd { get; init; }
    public DashboardSummaryDto Summary { get; init; } = default!;
    public List<RevenueTrendDto> RevenueTrend { get; init; } = [];
    public List<OrderVolumeDto> OrderVolume { get; init; } = [];
    public List<SalesByCategoryDto> SalesByCategory { get; init; } = [];
    public List<UserActivityDto> UserActivity { get; init; } = [];
    public PaginatedResult<ProductPerformanceRow> ProductPerformance { get; init; } = new();
    public PaginatedResult<TopSellerRow> TopSellers { get; init; } = new();
}