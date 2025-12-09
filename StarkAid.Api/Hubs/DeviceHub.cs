using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarkAid.Api.Hubs;

/// <summary>
/// Hub para receber status de dispositivos e enviar comandos em tempo real.
/// </summary>
[Authorize] // requer JWT
public class DeviceHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     Context.User?.FindFirstValue("sub");

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            await Clients.Caller.SendAsync("Connected", "CONNECTED");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     Context.User?.FindFirstValue("sub");

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Echo simples (pode ser usado para testes)
    public async Task SendMessage(string message) =>
        await Clients.Caller.SendAsync("Echo", message);
}
