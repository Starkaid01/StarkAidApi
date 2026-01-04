using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace StarkAid.Api.Hubs;

public class AvatarHub : Hub
{
    private string ResolveSessionId(string? querySessionId)
    {
        var sessionId = string.IsNullOrWhiteSpace(querySessionId) ? null : querySessionId.Trim();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return sessionId;
        }

        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     Context.User?.FindFirstValue("sub");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }

        return "default";
    }

    public async Task StopSpeaking()
    {
        var sessionId = ResolveSessionId(Context.GetHttpContext()?.Request.Query["session"].ToString());
        await Clients.Group(sessionId).SendAsync("StopSpeaking");
    }

    public override async Task OnConnectedAsync()
    {
        var sessionId = ResolveSessionId(Context.GetHttpContext()?.Request.Query["session"].ToString());

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        await Clients.Caller.SendAsync("Connected", sessionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sessionId = ResolveSessionId(Context.GetHttpContext()?.Request.Query["session"].ToString());

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        await base.OnDisconnectedAsync(exception);
    }
}
