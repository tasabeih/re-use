using ReUse.Domain.Enums;

namespace ReUse.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = default!;

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = default!;

    public MessageType MessageType { get; set; }

    // Content is null for Media-only messages.
    // MediaUrl is null for Text messages.
    // Both can be set simultaneously (e.g. image with a caption).
    public string? Content { get; set; }
    public string? MediaUrl { get; set; }

    // Read-tracking — no separate enum to avoid conflict with the existing DeliveryStatus
    // enum used by the notification system.
    //
    //   DeliveredAt == null  → message saved but receiver not yet online
    //   DeliveredAt != null  → client acknowledged via SignalR JoinConversation
    //   ReadAt == null       → not yet read
    //   ReadAt != null       → receiver opened the conversation and saw it
    public DateTime SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Soft-delete per participant.
    // When BOTH are true the message is excluded from all queries entirely.
    // When only one is true, the other participant still sees it.
    public bool IsDeletedBySender { get; set; }
    public bool IsDeletedByReceiver { get; set; }
}