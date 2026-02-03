using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Volo.Abp.Users;

namespace LabSpecimenManagement.Hubs;

/// <summary>
/// SignalR Hub cho Chat realtime
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        ICurrentUser currentUser,
        ILogger<ChatHub> logger)
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUser.Id;
        if (userId.HasValue)
        {
            // Join personal group for direct notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"agent_{userId}");

            // Join all_agents group for broadcast
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_agents");

            _logger.LogInformation("Agent {UserId} connected to ChatHub", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUser.Id;
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"agent_{userId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_agents");

            _logger.LogInformation("Agent {UserId} disconnected from ChatHub", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Agent join conversation room để nhận messages
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogDebug("Agent {UserId} joined conversation {ConversationId}",
            _currentUser.Id, conversationId);
    }

    /// <summary>
    /// Agent leave conversation room
    /// </summary>
    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogDebug("Agent {UserId} left conversation {ConversationId}",
            _currentUser.Id, conversationId);
    }

    /// <summary>
    /// Agent typing indicator
    /// </summary>
    public async Task SendTyping(Guid conversationId)
    {
        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("AgentTyping", new
            {
                ConversationId = conversationId,
                AgentId = _currentUser.Id,
                AgentName = _currentUser.Name ?? _currentUser.UserName
            });
    }
}
