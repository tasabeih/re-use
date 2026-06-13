namespace ReUse.Application.DTOs.Chat.Requests;

public record StartConversationRequest(
    // Optional opening message sent immediately when the thread is created.
    // If null the conversation is created empty — the UI opens blank.
    string? InitialMessage
);