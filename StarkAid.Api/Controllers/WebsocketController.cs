using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StarkAid.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // JWT obrigatório (o pipeline já faz a validação antes de executar a action)
    public class WebsocketController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, WebSocket> _connections =
            new ConcurrentDictionary<string, WebSocket>(StringComparer.Ordinal);

        // wss://.../api/Websocket/connect/{userId}
        [HttpGet("connect/{userId}")]
        public async Task Connect(string userId)
        {
            // 1) Precisa ser WebSocket
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // 2) Garante que o userId do token == rota
            var tokenUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(tokenUserId) || !string.Equals(tokenUserId, userId, StringComparison.Ordinal))
            {
                HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await HttpContext.Response.WriteAsync("userId do token diferente do da rota.");
                return;
            }

            // 3) Aceita o WebSocket após autenticação/autorização
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            // Substitui conexão anterior (se houver) do mesmo usuário
            _connections.AddOrUpdate(userId, webSocket, (_, __) => webSocket);
            Console.WriteLine($"✅ WS conectado: {userId}");

            // Confirmação imediata
            await SafeSendAsync(webSocket, "CONNECTED");

            try
            {
                await ReceiveLoop(userId, webSocket);
            }
            finally
            {
                RemoveConnection(userId);
                await SafeCloseAsync(webSocket, WebSocketCloseStatus.NormalClosure, "Closing");
                Console.WriteLine($"❌ WS desconectado: {userId}");
            }
        }

        private static async Task ReceiveLoop(string userId, WebSocket webSocket)
        {
            var buffer = new byte[4 * 1024];
            var builder = new StringBuilder();

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erro Receive ({userId}): {ex.Message}");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Cliente pediu fechamento
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        var message = builder.ToString();
                        builder.Clear();

                        Console.WriteLine($"📩 [{userId}] {message}");

                        if (string.Equals(message, "ping", StringComparison.OrdinalIgnoreCase))
                        {
                            await SendToUser(userId, "pong");
                        }
                        else
                        {
                            // Echo/roteamento de comandos aqui se quiser
                            await SendToUser(userId, $"Echo: {message}");
                        }
                    }
                }
            }
        }

        public static void RemoveConnection(string userId)
        {
            _connections.TryRemove(userId, out _);
        }

        public static async Task SendToUser(string userId, string message)
        {
            if (_connections.TryGetValue(userId, out var socket))
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await SafeSendAsync(socket, message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ SendToUser falhou ({userId}): {ex.Message}");
                        RemoveConnection(userId);
                    }
                }
                else
                {
                    RemoveConnection(userId);
                }
            }
        }

        [HttpPost("broadcast")]
        [Authorize(Roles = "Administrador")] // opcional: se quiser exigir JWT para broadcast, troque para [Authorize(Roles="Administrador")]
        public async Task<IActionResult> Broadcast([FromBody] MessageModel model)
        {
            Console.WriteLine($"📢 Broadcast: {model?.Message} (conexões: {_connections.Count})");
            await SendToAll(model?.Message ?? string.Empty);
            return Ok(new { result = "Enviado" });
        }

        [HttpPost("simple-broadcast")]
        [AllowAnonymous]
        public async Task<IActionResult> SimpleBroadcast()
        {
            var message = "TESTE SIMPLES " + DateTime.Now.ToString("O");
            Console.WriteLine($"📢 {message}");
            await SendToAll(message);
            return Ok();
        }

        private static async Task SendToAll(string message)
        {
            foreach (var kv in _connections)
            {
                var userId = kv.Key;
                var socket = kv.Value;

                if (socket.State != WebSocketState.Open)
                {
                    RemoveConnection(userId);
                    continue;
                }

                try
                {
                    await SafeSendAsync(socket, message);
                    Console.WriteLine($"➡️ Enviado para {userId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Falha ao enviar para {userId}: {ex.Message}");
                    RemoveConnection(userId);
                }
            }
        }

        private static async Task SafeSendAsync(WebSocket socket, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task SafeCloseAsync(WebSocket socket, WebSocketCloseStatus status, string description)
        {
            try
            {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(status, description, CancellationToken.None);
                }
            }
            catch { /* ignore */ }
        }

        public class MessageModel
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
