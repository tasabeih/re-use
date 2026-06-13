namespace ReUse.Application.DTOs.Chat.Responses;

/// <summary>
/// Returned by GET /conversations/{id}.
/// Contains conversation metadata + first page of messages in one response
/// so the UI can render the full chat screen in a single HTTP call.
/// </summary>
public record ConversationDetailResponse
{
    public ConversationResponse Conversation { get; init; } = null!;
    public PagedResult<MessageResponse> Messages { get; init; } = null!;
}