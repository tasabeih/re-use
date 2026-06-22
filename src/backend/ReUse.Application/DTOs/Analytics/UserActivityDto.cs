namespace ReUse.Application.DTOs.Analytics;

public record UserActivityDto
{
    public string Week { get; init; } = string.Empty;
    public int NewUsers { get; init; }
}