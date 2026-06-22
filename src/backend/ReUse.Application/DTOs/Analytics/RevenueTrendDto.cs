namespace ReUse.Application.DTOs.Analytics;

public record RevenueTrendDto
{
    public string Month { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
}