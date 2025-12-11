using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.Suporte;
using StarkAid.Api.Services.V1.Email;
using System.Security.Claims;

namespace StarkAid.Api.Controllers.V1;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/suporte")]
public class SuporteController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public SuporteController(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [HttpGet("links-uteis")]
    [AllowAnonymous]
    public IActionResult GetLinksUteis()
    {
        // Aqui você pode buscar essas URLs do banco de dados ou configuração
        // Por enquanto, retornando URLs relativas que podem ser configuradas
        var baseUrl = Request.Scheme + "://" + Request.Host;
        
        var links = new LinksUteisResponse
        {
            ExcluirConta = $"{baseUrl}/delete-acc/delete-account.html",
            CancelarPlanos = $"{baseUrl}/delete-acc/cancelar-assinatura.html",
            MudarSenha = $"{baseUrl}/password/reset-password.html",
            ResetarSenha = $"{baseUrl}/password/reset-password.html",
            AdicionarFundos = $"{baseUrl}/checkout-page.html",
            HistoricoPagamentos = $"{baseUrl}/automacao.html#admin-vendas",
            AbrirSuporte = $"{baseUrl}/automacao.html#admin-manutencao",
            TermosEPoliticas = $"{baseUrl}/starkaid-privacy/privacy.html"
        };

        return Ok(links);
    }

    [HttpPost("enviar-formulario-limite")]
    [Authorize]
    public async Task<IActionResult> EnviarFormularioLimite([FromBody] FormularioLimiteRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound("Usuário não encontrado.");
        }

        // Enviar email para suporte
        var assunto = $"Limite de Chat Atingido - Usuário: {user.Name} ({user.Email})";
        var corpo = $@"
Usuário: {user.Name}
Email: {user.Email}
UserId: {userId}

Mensagem do usuário:
{request.Mensagem}

Detalhes adicionais:
{request.Detalhes}
";

        try
        {
            await _emailService.SendAsync("starkaid24@gmail.com", assunto, corpo);
            return Ok(new { message = "Formulário enviado com sucesso. Você receberá instruções por email em breve." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro ao enviar formulário.", error = ex.Message });
        }
    }

    [HttpGet("verificar-resolvendo-suporte")]
    [Authorize]
    public async Task<IActionResult> VerificarResolvendoSuporte()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var origem = Request.Query["origem"].FirstOrDefault() ?? "software";

        var resolvendoSuporte = await _context.ResolvendoSuportes
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Origem == origem && r.Ativo);

        if (resolvendoSuporte != null)
        {
            return Ok(new { ativo = true, message = "Você estava em processo de resolução de suporte. O problema foi resolvido?" });
        }

        return Ok(new { ativo = false });
    }

    [HttpPost("marcar-resolvido")]
    [Authorize]
    public async Task<IActionResult> MarcarResolvido([FromBody] MarcarResolvidoRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var origem = request.Origem ?? "software";

        var resolvendoSuporte = await _context.ResolvendoSuportes
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Origem == origem && r.Ativo);

        if (resolvendoSuporte != null)
        {
            resolvendoSuporte.Ativo = false;
            resolvendoSuporte.ResolvidoEm = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Status atualizado." });
    }
}

public class FormularioLimiteRequest
{
    public string Mensagem { get; set; } = string.Empty;
    public string? Detalhes { get; set; }
}

public class MarcarResolvidoRequest
{
    public string? Origem { get; set; }
}
