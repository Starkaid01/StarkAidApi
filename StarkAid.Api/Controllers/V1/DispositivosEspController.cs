using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using StarkAid.Api.DTOs.V1.DispositivoEsp;
using StarkAid.Api.Hubs;
using StarkAid.Api.Services.V1.DispositivoEsp;
using System.Security.Claims;

namespace StarkAid.Api.Controllers.V1;

[Authorize]
[ApiVersion("1.0")]
[EnableRateLimiting("UserRateLimit")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class DispositivosEspController : ControllerBase
{
    private readonly DispositivoEspService _service;
    private readonly IHubContext<DispositivoEspHub> _hubContext;
    private readonly ILogger<DispositivosEspController> _logger;

    public DispositivosEspController(
        DispositivoEspService service,
        IHubContext<DispositivoEspHub> hubContext,
        ILogger<DispositivosEspController> logger)
    {
        _service = service;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = null;
        
        if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var dispositivos = await _service.GetAllAsync(userId);
        return Ok(dispositivos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dispositivo = await _service.GetByIdAsync(id);
        if (dispositivo == null) return NotFound();

        return Ok(dispositivo);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDispositivoEspRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Ip))
            return BadRequest("Nome e IP são obrigatórios.");

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = null;
        
        if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var dispositivo = await _service.CreateAsync(
            request.Nome,
            request.Ip,
            request.Porta,
            request.Comando,
            request.ComandToEsp,
            userId
        );

        return CreatedAtAction(nameof(GetById), new { id = dispositivo.Id }, dispositivo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDispositivoEspRequest request)
    {
        // Buscar dispositivo antes de atualizar para verificar mudança de status
        var dispositivoAntes = await _service.GetByIdAsync(id);
        if (dispositivoAntes == null) return NotFound();

        var ligadoDesligadoAntes = dispositivoAntes.LigadoDesligado;
        var comandoAntes = dispositivoAntes.Comando;

        var success = await _service.UpdateAsync(
            id,
            request.Nome,
            request.Ip,
            request.Porta,
            request.Comando,
            request.ComandToEsp,
            request.Status,
            request.LigadoDesligado
        );

        if (!success) return NotFound();

        // Buscar dispositivo atualizado
        var dispositivo = await _service.GetByIdAsync(id);
        if (dispositivo == null) return NotFound();

        // Verificar se LigadoDesligado mudou e se há comando configurado
        if (request.LigadoDesligado.HasValue && 
            request.LigadoDesligado.Value != ligadoDesligadoAntes)
        {
            // Usa ComandToEsp se disponível, senão usa Comando como fallback
            var comandoParaEnviar = !string.IsNullOrWhiteSpace(dispositivo.ComandToEsp) 
                ? dispositivo.ComandToEsp 
                : dispositivo.Comando;

            if (string.IsNullOrWhiteSpace(comandoParaEnviar))
            {
                _logger.LogWarning("Dispositivo {Nome} não tem comando configurado (ComandToEsp ou Comando)", dispositivo.Nome);
            }
            else
            {
                var comandoData = new
                {
                    nome = dispositivo.Nome,
                    ip = dispositivo.Ip,
                    porta = dispositivo.Porta,
                    comando = comandoParaEnviar,
                    comandToEsp = dispositivo.ComandToEsp ?? comandoParaEnviar
                };
                
                _logger.LogInformation("Enviando comando via WebSocket - Nome: {Nome}, IP: {Ip}, Porta: {Porta}, Comando: {Comando}, ComandToEsp: {ComandToEsp}", 
                    comandoData.nome, comandoData.ip, comandoData.porta, comandoData.comando, comandoData.comandToEsp);
                
                // Enviar comando via WebSocket para o software Windows Forms
                await _hubContext.Clients.Group("type_software").SendAsync("ComandoDispositivo", comandoData);

                _logger.LogInformation("Comando '{Comando}' enviado para dispositivo {Nome} ({Ip}:{Porta}) após mudança de status", 
                    comandoParaEnviar, dispositivo.Nome, dispositivo.Ip, dispositivo.Porta);
            }
        }

        // Notifica atualização via WebSocket
        await _hubContext.Clients.All.SendAsync("StatusDispositivoAtualizado", new
        {
            nome = dispositivo.Nome,
            ip = dispositivo.Ip,
            status = dispositivo.Status,
            ligadoDesligado = dispositivo.LigadoDesligado
        });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/ping")]
    public async Task<IActionResult> Ping(Guid id)
    {
        var isOnline = await _service.PingAsync(id);
        var dispositivo = await _service.GetByIdAsync(id);
        
        if (dispositivo == null) return NotFound();

        // Notifica atualização de status via WebSocket
        await _hubContext.Clients.All.SendAsync("StatusDispositivoAtualizado", new
        {
            nome = dispositivo.Nome,
            ip = dispositivo.Ip,
            status = dispositivo.Status,
            ligadoDesligado = dispositivo.LigadoDesligado
        });

        return Ok(new { status = dispositivo.Status, isOnline });
    }

    [HttpPost("enviar-comando")]
    public async Task<IActionResult> EnviarComando([FromBody] EnviarComandoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Comando))
            return BadRequest("Comando é obrigatório.");

        // Busca dispositivos que correspondem ao comando
        var dispositivos = await _service.FindByComandoPartialAsync(request.Comando);

        if (dispositivos.Count == 0)
            return NotFound("Nenhum dispositivo encontrado para o comando especificado.");

        // Envia comando para o primeiro dispositivo encontrado (ou pode enviar para todos)
        var dispositivo = dispositivos.First();

        // Usa ComandToEsp se disponível, senão usa Comando como fallback
        var comandoParaEnviar = !string.IsNullOrWhiteSpace(dispositivo.ComandToEsp) 
            ? dispositivo.ComandToEsp 
            : (!string.IsNullOrWhiteSpace(dispositivo.Comando) ? dispositivo.Comando : request.Comando);

        if (string.IsNullOrWhiteSpace(comandoParaEnviar))
        {
            return BadRequest("Dispositivo não tem comando configurado (ComandToEsp ou Comando).");
        }

        // Envia comando via WebSocket para o software Windows Forms
        var comandoData = new
        {
            nome = dispositivo.Nome,
            ip = dispositivo.Ip,
            porta = dispositivo.Porta,
            comando = comandoParaEnviar,
            comandToEsp = dispositivo.ComandToEsp ?? comandoParaEnviar
        };

        _logger.LogInformation("Enviando comando via WebSocket para grupo 'type_software': Nome={Nome}, IP={Ip}, Porta={Porta}, Comando={Comando}, ComandToEsp={ComandToEsp}", 
            comandoData.nome, comandoData.ip, comandoData.porta, comandoData.comando, comandoData.comandToEsp);

        await _hubContext.Clients.Group("type_software").SendAsync("ComandoDispositivo", comandoData);

        _logger.LogInformation("Comando '{Comando}' enviado para dispositivo {Nome} ({Ip}:{Porta})", 
            comandoParaEnviar, dispositivo.Nome, dispositivo.Ip, dispositivo.Porta);

        return Ok(new
        {
            dispositivo = new
            {
                dispositivo.Nome,
                dispositivo.Ip,
                dispositivo.Porta,
                dispositivo.Comando,
                dispositivo.ComandToEsp
            },
            comandoEnviado = comandoParaEnviar,
            mensagem = "Comando enviado com sucesso"
        });
    }

    [HttpPost("ping-all")]
    [Authorize(Roles = "Administrador,userAdmin")]
    public async Task<IActionResult> PingAll()
    {
        var dispositivos = await _service.GetAllAsync();
        var results = new List<object>();

        foreach (var dispositivo in dispositivos)
        {
            var isOnline = await _service.PingAsync(dispositivo.Id);
            results.Add(new
            {
                dispositivo.Id,
                dispositivo.Nome,
                dispositivo.Ip,
                status = dispositivo.Status,
                isOnline
            });
        }

        return Ok(results);
    }
}

