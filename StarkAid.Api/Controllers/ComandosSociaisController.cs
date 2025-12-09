using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.DTOs.SocialCommand;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.SocialCommand;
using System.Security.Claims;

namespace StarkAid.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
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
        return Ok(comandos);
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

            return Created("", novo);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Saldo insuficiente"))
        {
            return BadRequest("Saldo insuficiente para criar o comando.");
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
            return Ok(respostasAleatorias);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Saldo insuficiente"))
        {
            return BadRequest("Saldo insuficiente para gerar variações.");
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

            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Saldo insuficiente"))
        {
            return BadRequest("Saldo insuficiente para atualizar comando.");
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

        return NoContent();
    }
}
