using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Services.V1;
using System.Net.WebSockets;
using System.Text;

namespace StarkAid.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("ws/transcribe")]
    public class TranscribeController : ControllerBase
{
    private readonly TranscribeProxyService _proxy;
    private readonly AppDbContext _context;

    public TranscribeController(TranscribeProxyService proxy, AppDbContext context)
    {
        _proxy = proxy;
        _context = context;
    }

    [HttpGet]
    public async Task Get([FromQuery] string apiKey, [FromQuery] string language = "pt-BR")
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsync("API key obrigatória.");
            return;
        }

        // 🔐 Verifica usuário
        var user = await _context.Users.FirstOrDefaultAsync(u => u.ApiKey == apiKey);
        if (user == null)
        {
            HttpContext.Response.StatusCode = 403;
            await HttpContext.Response.WriteAsync("API key inválida.");
            return;
        }

        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

        // Mensagem inicial de autenticação OK (com pacote econômico)
        if (ws.State == WebSocketState.Open)
        {
            await _proxy.SendAuthOkAsync(ws, user.Id);
        }

        // 🔹 Passa o userId para o proxy
        await _proxy.StartTranscriptionAsync(ws, language, user.Id);
    }
}
}
