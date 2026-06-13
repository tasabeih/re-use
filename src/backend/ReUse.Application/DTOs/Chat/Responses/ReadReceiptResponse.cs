namespace ReUse.Application.DTOs.Chat.Responses;

/// <summary>
/// Pushed via SignalR to the sender when the receiver reads their messages.
/// The sender's UI uses this to show the "seen" tick on their messages.
/// </summary>
public record ReadReceiptResponse
{
    public Guid ConversationId { get; init; }
    public Guid ReadByUserId { get; init; }
    public DateTime ReadAt { get; init; }
}