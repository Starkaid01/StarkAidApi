using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarkAid.Api.Hubs
{
    [Authorize] // JWT obrigatório
    public class DeviceHub : Hub
    {

        // Executado quando o cliente se conecta
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? Context.User?.FindFirstValue("sub");

            if (!string.IsNullOrEmpty(userId))
            {
                // Adiciona o usuário a um grupo com seu userId
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                Console.WriteLine($"✅ Hub conectado: {userId} | ConnectionId: {Context.ConnectionId}");

                // Confirmação imediata
                await Clients.Caller.SendAsync("Connected", "CONNECTED");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? Context.User?.FindFirstValue("sub");

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                Console.WriteLine($"❌ Hub desconectado: {userId} | ConnectionId: {Context.ConnectionId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Recebe mensagem do cliente
        public async Task SendMessage(string message)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? Context.User?.FindFirstValue("sub");

            Console.WriteLine($"📩 [{userId}] {message}");

            if (string.Equals(message, "ping", StringComparison.OrdinalIgnoreCase))
            {
                await Clients.Caller.SendAsync("Pong", "pong");
            }
            else
            {
                // Echo simples
                await Clients.Caller.SendAsync("Echo", message);
            }
        }

        // Envia mensagem para um usuário específico
        public async Task SendToUser(string targetUserId, string message)
        {
            await Clients.Group(targetUserId).SendAsync("ReceiveMessage", message);
        }

        // Broadcast para todos conectados
        public async Task Broadcast(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

        public async Task SendCommandToUser(string deviceId, string command)
        {
            var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? Context.User?.FindFirstValue("sub");

            if (!string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine($"📩 [{userIdClaim}] Comando: {command} para device {deviceId}");
                await Clients.Group(userIdClaim).SendAsync("ReceiveCommand", deviceId, command);
            }
        }
    }
}
