namespace ReUse.Application.DTOs.Analytics;

public record TopSellerRow
{
    public int Rank { get; init; }
    public string SellerName { get; init; } = string.Empty;
    public int ProductCount { get; init; }
    public int TotalSales { get; init; }
    public decimal Revenue { get; init; }
    public double Rating { get; init; }
    public string Performance { get; init; } = string.Empty;
}