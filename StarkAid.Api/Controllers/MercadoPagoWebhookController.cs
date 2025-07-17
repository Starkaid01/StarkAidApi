using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs;

namespace StarkAid.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MercadoPagoWebhookController : ControllerBase
{
    private readonly AppDbContext _context;

    public MercadoPagoWebhookController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> ReceberWebhook([FromBody] MercadoPagoWebhookDto dto)
    {
        // Para debug inicial
        Console.WriteLine($"Webhook recebido: {dto.Action}, {dto.Type}, {dto.DataId}");

        if (dto.Type == "preapproval" && dto.Action == "authorized")
        {
            var userId = Guid.Parse(dto.DataId);
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Role = "UserNivel2";
                user.UltimoPagamentoConfirmadoEm = DateTime.UtcNow; // <- aqui atualiza a data
                await _context.SaveChangesAsync();
            }
        }

        return Ok();
    }
}
