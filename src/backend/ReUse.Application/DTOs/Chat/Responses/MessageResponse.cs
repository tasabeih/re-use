using ReUse.Domain.Enums;

namespace ReUse.Application.DTOs.Chat.Responses;

public record MessageResponse
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }

    // Sender info
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public string? SenderAvatarUrl { get; init; }

    // Content
    public MessageType MessageType { get; init; }
    public string? Content { get; init; }
    public string? MediaUrl { get; init; }

    // Offer-specific — only set when MessageType == Offer
    public decimal? OfferPrice { get; init; }

    // Read tracking
    public DateTime SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? ReadAt { get; init; }

    // Soft-delete flags — client uses these to show "message deleted" placeholder
    public bool IsDeletedBySender { get; init; }
    public bool IsDeletedByReceiver { get; init; }
}