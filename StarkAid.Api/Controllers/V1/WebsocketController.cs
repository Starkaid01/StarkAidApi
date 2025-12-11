using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StarkAid.Api.Controllers.V1;

[Authorize]
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class WebsocketController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, WebSocket> _connections =
        new ConcurrentDictionary<string, WebSocket>(StringComparer.Ordinal);

    // GET api/websocket/connect/{userId}
    [HttpGet("connect/{userId}")]
    public async Task Connect(string userId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var tokenUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                          User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(tokenUserId) || tokenUserId != userId)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            await HttpContext.Response.WriteAsync("userId do token não corresponde ao da rota.");
            return;
        }

        var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _connections.AddOrUpdate(userId, socket, (_, __) => socket);
        await SendAsync(socket, "CONNECTED");

        try
        {
            var buffer = new byte[4 * 1024];
            var sb = new StringBuilder();

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (result.EndOfMessage)
                    {
                        var msg = sb.ToString();
                        sb.Clear();

                        if (msg.Equals("ping", StringComparison.OrdinalIgnoreCase))
                            await SendToUser(userId, "pong");
                        else
                            await SendToUser(userId, $"Echo: {msg}");
                    }
                }
            }
        }
        finally
        {
            _connections.TryRemove(userId, out _);
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
        }
    }

    // POST api/websocket/broadcast  (admin only)
    [HttpPost("broadcast")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastModel model)
    {
        foreach (var kv in _connections)
        {
            if (kv.Value.State == WebSocketState.Open)
                await SendAsync(kv.Value, model.Message);
        }
        return Ok(new { result = "Enviado" });
    }

    private static async Task SendToUser(string userId, string message)
    {
        if (_connections.TryGetValue(userId, out var socket) && socket.State == WebSocketState.Open)
        {
            await SendAsync(socket, message);
        }
    }

    private static async Task SendAsync(WebSocket socket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public class BroadcastModel
    {
        public string Message { get; set; } = string.Empty;
    }
}
