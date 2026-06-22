namespace ReUse.Application.DTOs.Analytics;

public record SalesByCategoryDto
{
    public string CategoryName { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal Revenue { get; init; }
    public double Percentage { get; init; }
}