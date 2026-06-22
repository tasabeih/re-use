namespace ReUse.Application.DTOs.Analytics;

public record ProductPerformanceRow
{
    public int Rank { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int Sales { get; init; }
    public decimal Revenue { get; init; }
    public int Views { get; init; }
    public string Conversion { get; init; } = string.Empty;
}