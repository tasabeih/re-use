namespace ReUse.Application.DTOs.Analytics;

public record DashboardSummaryDto
{
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public decimal AvgOrderValue { get; init; }
    public int TotalUsers { get; init; }
    public int ActiveProducts { get; init; }
}