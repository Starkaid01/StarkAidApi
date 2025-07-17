using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Data;
using StarkAid.Api.Services;
using System.Security.Claims;

namespace StarkAid.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssinaturasController : ControllerBase
{
    private readonly MercadoPagoService _mercadoPagoService;
    private readonly AppDbContext _context;

    public AssinaturasController(MercadoPagoService mercadoPagoService, AppDbContext context)
    {
        _mercadoPagoService = mercadoPagoService;
        _context = context;
    }

    [Authorize]
    [HttpPost("criar-assinatura")]
    public async Task<IActionResult> CriarAssinatura([FromQuery] decimal valor)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.FindAsync(Guid.Parse(userId));

        if (user == null)
            return NotFound("Usuário não encontrado.");

        

        var assinaturaResponse = await _mercadoPagoService.CriarAssinaturaAsync(user.Email, valor, user.Id);
        user.PreapprovalId = assinaturaResponse.PreapprovalId;
        await _context.SaveChangesAsync();

        return Ok(assinaturaResponse);
    }
}
