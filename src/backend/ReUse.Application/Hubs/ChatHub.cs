using System.Collections.Concurrent;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using ReUse.Application.DTOs.Chat.Responses;
using ReUse.Application.Interfaces.Services;

namespace ReUse.Application.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IConversationService _conversationService;

    // In-memory map: userId → set of connectionIds
    // Static so it survives across Hub instances (one instance per invocation)
    private static readonly ConcurrentDictionary<Guid, HashSet<string>> _userConnections
        = new();

    private static readonly object _lock = new();

    public ChatHub(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var userId = GetCallerId();

        lock (_lock)
        {
            if (!_userConnections.ContainsKey(userId))
                _userConnections[userId] = new HashSet<string>();

            _userConnections[userId].Add(Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCallerId();

        lock (_lock)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                    _userConnections.TryRemove(userId, out _);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── Client → Server ──────────────────────────────────────────────────────

    public async Task JoinConversation(Guid conversationId)
    {
        var callerId = GetCallerId();

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));

        var updatedCount = await _conversationService.MarkAsReadAsync(conversationId, callerId);

        if (updatedCount > 0)
        {
            var receipt = new ReadReceiptResponse
            {
                ConversationId = conversationId,
                ReadByUserId = callerId,
                ReadAt = DateTime.UtcNow
            };

            await Clients
                .OthersInGroup(ConversationGroup(conversationId))
                .SendAsync("ReadReceipt", receipt);
        }
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId, ConversationGroup(conversationId));
    }

    // ── Static helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active connection IDs for a user.
    /// Used by the controller to call GroupExcept with real connection IDs.
    /// </summary>
    public static IReadOnlyList<string> GetConnectionIds(Guid userId)
    {
        lock (_lock)
        {
            return _userConnections.TryGetValue(userId, out var ids)
                ? ids.ToList()
                : [];
        }
    }

    public static string ConversationGroup(Guid conversationId)
        => $"conversation-{conversationId}";

    // ── Private ──────────────────────────────────────────────────────────────

    private Guid GetCallerId()
    {
        var value = Context.User?.FindFirstValue("business_user_id");
        if (string.IsNullOrEmpty(value))
            throw new HubException("Unauthorized.");
        return Guid.Parse(value);
    }
}