using ReUse.Domain.Enums;

namespace ReUse.Application.DTOs.Chat.Responses;

public record ConversationResponse
{
    public Guid Id { get; init; }

    // Product info — displayed in the conversation header
    public Guid ProductId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string? ProductCoverImageUrl { get; init; }
    public ProductStatus ProductStatus { get; init; }

    // Participant info
    public Guid BuyerId { get; init; }
    public string BuyerName { get; init; } = string.Empty;
    public string? BuyerAvatarUrl { get; init; }

    public Guid SellerId { get; init; }
    public string SellerName { get; init; } = string.Empty;
    public string? SellerAvatarUrl { get; init; }

    // Conversation state
    public ConversationType ConversationType { get; init; }
    public ConversationStatus Status { get; init; }
    public bool IsActive { get; init; }
    public DateTime LastActivityAt { get; init; }
    public DateTime CreatedAt { get; init; }

    // Inbox preview — shown in the conversation list
    public string? LastMessagePreview { get; init; }
    public int UnreadCount { get; init; }
}