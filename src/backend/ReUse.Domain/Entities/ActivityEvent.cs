using System;

namespace ReUse.Domain.Entities;

public class ActivityEvent : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Metadata { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}\