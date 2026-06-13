using ReUse.Domain.Enums;

namespace ReUse.Application.DTOs.Chat.Responses;

/// <summary>
/// Flat projection returned by repository queries.
/// Carries the precomputed LastMessagePreview so AutoMapper
/// never needs to touch the Messages nav collection.
/// </summary>
public class ConversationProjection
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string? ProductCoverImageUrl { get; init; }
    public ProductStatus ProductStatus { get; init; }
    public Guid BuyerId { get; init; }
    public string BuyerName { get; init; } = string.Empty;
    public string? BuyerAvatarUrl { get; init; }
    public Guid SellerId { get; init; }
    public string SellerName { get; init; } = string.Empty;
    public string? SellerAvatarUrl { get; init; }
    public ConversationType ConversationType { get; init; }
    public ConversationStatus Status { get; init; }
    public bool IsActive { get; init; }
    public DateTime LastActivityAt { get; init; }
    public DateTime CreatedAt { get; init; }

    // Computed in SQL — never from nav collection
    public string? LastMessagePreview { get; init; }
    public MessageType? LastMessageType { get; init; }

    // Set by service after DB call (requires async count query)
    public int UnreadCount { get; set; }
}