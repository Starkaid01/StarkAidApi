using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarkAid.Api.Entities;
using StarkAid.Api.Services;
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
    public async Task<IActionResult> Post([FromBody] ComandoSocial request)
    {
        if (string.IsNullOrWhiteSpace(request.Comando) || string.IsNullOrWhiteSpace(request.Resposta))
            return BadRequest("Comando e resposta são obrigatórios.");

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Token inválido.");

        var novo = await _service.AddAsync(userId, request.Comando, request.Resposta);
        return Created("", novo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] ComandoSocial request)
    {
        if (string.IsNullOrWhiteSpace(request.Comando) || string.IsNullOrWhiteSpace(request.Resposta))
            return BadRequest("Comando e resposta são obrigatórios.");

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized("Token inválido.");

        var atualizado = await _service.EditAsync(id, userId, request.Comando, request.Resposta);
        if (!atualizado)
            return NotFound("Comando não encontrado ou pertence a outro usuário.");

        return NoContent();
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
