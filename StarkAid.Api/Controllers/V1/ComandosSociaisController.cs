using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StarkAid.Api.DTOs.V1.SocialCommand;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.SocialCommand;
using StarkAid.Api.Services;
using System.Security.Claims;

namespace StarkAid.Api.Controllers.V1;

[Authorize]
[ApiVersion("1.0")]
[EnableRateLimiting("UserRateLimit")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class ComandosSociaisController : ControllerBase
{
    private readonly ComandoSocialService _service;

    public ComandosSociaisController(ComandoSocialService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Token inválido.");

        var comandos = await _service.GetByUserIdAsync(userId);
        var economy = await _service.ObterEconomiaAsync(userId);
        return Ok(new { comandos, economy });
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ComandoSocialDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Comando) || string.IsNullOrWhiteSpace(request.Resposta))
            return BadRequest("Comando e resposta são obrigatórios.");

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Token inválido.");

        try
        {
            var novo = await _service.AddAsync(userId, request.Comando, request.Resposta, request.Estilo);
            if (novo == null)
                return StatusCode(500, "Erro ao gerar variações com a IA.");

            var economy = await _service.ObterEconomiaAsync(userId);
            return Created("", new { comando = novo, economy });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Saldo insuficiente"))
        {
            return BadRequest("Saldo insuficiente para criar o comando.");
        }
        catch (TokenInsufficientException tex)
        {
            return StatusCode(402, new { message = tex.Message, requiredCoins = tex.RequiredCoins });
        }
    }

    [HttpGet("random-answers")]
    public async Task<IActionResult> GetRandomAnswers([FromQuery] string resposta)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Token inválido.");

        try
        {
            var respostasAleatorias = await _service.RespsrandomAnswers(userId, resposta);
            if (respostasAleatorias == null)
                return StatusCode(500, "Erro ao gerar variações com a IA.");
            var economy = await _service.ObterEconomiaAsync(userId);
            return Ok(new { respostasAleatorias, economy });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Saldo insuficiente"))
        {
            return BadRequest("Saldo insuficiente para gerar variações.");
        }
        catch (TokenInsufficientException tex)
        {
            return StatusCode(402, new { message = tex.Message, requiredCoins = tex.RequiredCoins });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] ComandoSocialDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Comando) || string.IsNullOrWhiteSpace(request.Resposta))
            return BadRequest("Comando e resposta são obrigatórios.");

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Token inválido.");

        try
        {
            var atualizado = await _service.EditAsync(id, userId, request.Comando, request.Resposta, request.Estilo);
            if (!atualizado)
                return NotFound("Comando não encontrado ou saldo insuficiente.");

            var economy = await _service.ObterEconomiaAsync(userId);
            return Ok(new { updated = true, economy });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Saldo insuficiente"))
        {
            return BadRequest("Saldo insuficiente para atualizar comando.");
        }
        catch (TokenInsufficientException tex)
        {
            return StatusCode(402, new { message = tex.Message, requiredCoins = tex.RequiredCoins });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Token inválido.");

        var excluido = await _service.DeleteAsync(id, userId);
        if (!excluido)
            return NotFound("Comando não encontrado ou pertence a outro usuário.");

        var economy = await _service.ObterEconomiaAsync(userId);
        return Ok(new { deleted = true, economy });
    }
}
