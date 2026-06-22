namespace ReUse.Application.DTOs.Analytics;

public record OrderVolumeDto
{
    public string Month { get; init; } = string.Empty;
    public int Orders { get; init; }
}