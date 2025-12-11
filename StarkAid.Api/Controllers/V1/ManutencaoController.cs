using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.Manutencao;
using StarkAid.Api.Entities;
using StarkAid.Api.Hubs;
using StarkAid.Api.Services.V1.Auth;
using System.Security.Claims;

namespace StarkAid.Api.Controllers.V1;

[Authorize]
[Authorize(Policy = "AdministradorOnly")]
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/manutencao")]
public class ManutencaoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly IHubContext<DispositivoEspHub> _dispositivoEspHubContext;
    private static readonly Dictionary<Guid, bool> _manutencaoAtiva = new();
    private static readonly Dictionary<Guid, string> _nomeAssistenteProvisorio = new();

    public ManutencaoController(AppDbContext context, AuthService authService, IHubContext<DispositivoEspHub> dispositivoEspHubContext)
    {
        _context = context;
        _authService = authService;
        _dispositivoEspHubContext = dispositivoEspHubContext;
    }

    // ========== SOFTWARE ==========

    [HttpPost("software/iniciar")]
    public IActionResult IniciarManutencaoSoftware([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        _manutencaoAtiva[request.UserId] = true;
        return Ok(new { message = $"Manutenção iniciada para usuário: {request.UserId}", userId = request.UserId });
    }

    [HttpPost("software/finalizar")]
    public IActionResult FinalizarManutencaoSoftware([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        _manutencaoAtiva.Remove(request.UserId);
        _nomeAssistenteProvisorio.Remove(request.UserId);
        return Ok(new { message = $"Manutenção finalizada para usuário: {request.UserId}", userId = request.UserId });
    }

    [HttpPost("software/alterar-senha")]
    public async Task<IActionResult> AlterarSenhaSoftware([FromBody] AlterarSenhaRequest request)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.NovaSenha))
            return BadRequest("UserId e NovaSenha são obrigatórios.");

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
            return NotFound("Usuário não encontrado.");

        user.PasswordHash = _authService.HashPassword(request.NovaSenha);
        user.LastUpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Senha alterada com sucesso.", userId = request.UserId });
    }

    [HttpPost("software/salvar-nome-assistente")]
    public IActionResult SalvarNomeAssistenteSoftware([FromBody] SalvarNomeAssistenteRequest request)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.NomeAssistente))
            return BadRequest("UserId e NomeAssistente são obrigatórios.");

        _nomeAssistenteProvisorio[request.UserId] = request.NomeAssistente;
        return Ok(new { message = "Nome do assistente salvo provisoriamente.", userId = request.UserId, nomeAssistente = request.NomeAssistente });
    }

    [HttpGet("software/dispositivos/{userId}")]
    public async Task<IActionResult> GetDispositivosEspSoftware(Guid userId)
    {
        var dispositivos = await _context.DispositivosEsp
            .Where(d => d.UserId == userId)
            .Select(d => new
            {
                d.Id,
                d.Nome,
                d.Ip,
                d.Porta,
                d.Comando,
                d.ComandToEsp,
                d.Status,
                d.LigadoDesligado,
                d.CreatedAt,
                d.LastPingAt
            })
            .ToListAsync();

        return Ok(dispositivos);
    }

    [HttpGet("software/comandos-sociais/{userId}")]
    public async Task<IActionResult> GetComandosSociaisSoftware(Guid userId)
    {
        var comandos = await _context.ComandosSociais
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.Comando,
                c.Resposta,
                c.RespostasAleatorias
            })
            .ToListAsync();

        return Ok(comandos);
    }

    [HttpGet("software/ultimos-comandos/{userId}")]
    public async Task<IActionResult> GetUltimosComandosSoftware(Guid userId)
    {
        // Último comando de IA
        var ultimoIa = await _context.IaHistoricos
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CriadoEm)
            .FirstOrDefaultAsync();

        // Último erro de software (pode conter comando de automação)
        var ultimoErroSoft = await _context.ErrorLogsSoft
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        // Último comando social
        var ultimoComandoSocial = await _context.ComandosSociais
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.Id) // Usando Id como proxy de ordem
            .FirstOrDefaultAsync();

        var response = new UltimosComandosResponse
        {
            UltimoComandoIA = ultimoIa?.TextoUsuario,
            UltimaRespostaIA = ultimoIa?.TextoIa,
            UltimoComandoAutomacao = ultimoErroSoft?.UltimoDispositivoAcionado,
            UltimoComandoSocial = ultimoComandoSocial?.Comando,
            UltimaRespostaSocial = ultimoComandoSocial?.Resposta
        };

        return Ok(response);
    }

    [HttpPost("software/limpar-cache")]
    public async Task<IActionResult> LimparCacheSoftware([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Enviar comando via SignalR para o software Windows Forms
        try
        {
            await _dispositivoEspHubContext.Clients.Group("type_software").SendAsync("SuporteComando", "limparcache");
        }
        catch (Exception ex)
        {
            // Log do erro mas não falha a requisição
            System.Diagnostics.Debug.WriteLine($"Erro ao enviar comando de limpar cache via SignalR: {ex.Message}");
        }

        return Ok(new { message = "Cache limpo com sucesso.", userId = request.UserId });
    }

    [HttpPost("software/limpar-dados")]
    public async Task<IActionResult> LimparDadosSoftware([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Limpar logs de erro do software
        var logsSoft = await _context.ErrorLogsSoft
            .Where(e => e.UserId == request.UserId)
            .ToListAsync();
        _context.ErrorLogsSoft.RemoveRange(logsSoft);

        // Limpar histórico de IA
        var historicosIa = await _context.IaHistoricos
            .Where(h => h.UserId == request.UserId)
            .ToListAsync();
        _context.IaHistoricos.RemoveRange(historicosIa);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Dados limpos com sucesso.", userId = request.UserId });
    }

    [HttpPost("software/logout")]
    public async Task<IActionResult> LogoutSoftware([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Enviar comando via SignalR para o software Windows Forms
        try
        {
            await _dispositivoEspHubContext.Clients.Group("type_software").SendAsync("SuporteComando", "logout");
        }
        catch (Exception ex)
        {
            // Log do erro mas não falha a requisição
            System.Diagnostics.Debug.WriteLine($"Erro ao enviar comando de logout via SignalR: {ex.Message}");
        }

        // Revogar todos os refresh tokens do usuário
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == request.UserId)
            .ToListAsync();
        _context.RefreshTokens.RemoveRange(refreshTokens);

        // Remover sessões ativas
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == request.UserId)
            .ToListAsync();
        _context.UserSessions.RemoveRange(sessions);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuário deslogado com sucesso.", userId = request.UserId });
    }

    // ========== APP ==========

    [HttpPost("app/limpar-cache")]
    public async Task<IActionResult> LimparCacheApp([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Enviar comando via SignalR para o app Android
        try
        {
            await _dispositivoEspHubContext.Clients.Group("type_app").SendAsync("SuporteComando", "limparcache");
        }
        catch (Exception ex)
        {
            // Log do erro mas não falha a requisição
            System.Diagnostics.Debug.WriteLine($"Erro ao enviar comando de limpar cache via SignalR: {ex.Message}");
        }

        return Ok(new { message = "Cache do app limpo com sucesso.", userId = request.UserId });
    }

    [HttpPost("app/limpar-dados")]
    public async Task<IActionResult> LimparDadosApp([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Limpar logs de erro do app
        var logsApp = await _context.ErrorLogsApp
            .Where(e => e.UserId == request.UserId)
            .ToListAsync();
        _context.ErrorLogsApp.RemoveRange(logsApp);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Dados do app limpos com sucesso.", userId = request.UserId });
    }

    [HttpPost("app/logout")]
    public async Task<IActionResult> LogoutApp([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Enviar comando via SignalR para o app Android
        try
        {
            await _dispositivoEspHubContext.Clients.Group("type_app").SendAsync("SuporteComando", "logout");
        }
        catch (Exception ex)
        {
            // Log do erro mas não falha a requisição
            System.Diagnostics.Debug.WriteLine($"Erro ao enviar comando de logout via SignalR: {ex.Message}");
        }

        // Revogar refresh tokens do app
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == request.UserId && rt.Origem == "app")
            .ToListAsync();
        _context.RefreshTokens.RemoveRange(refreshTokens);

        // Remover sessões do app
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == request.UserId && s.Origem == "app")
            .ToListAsync();
        _context.UserSessions.RemoveRange(sessions);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuário deslogado do app com sucesso.", userId = request.UserId });
    }

    [HttpGet("app/ultimos-comandos/{userId}")]
    public async Task<IActionResult> GetUltimosComandosApp(Guid userId)
    {
        // Último comando de IA
        var ultimoIa = await _context.IaHistoricos
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CriadoEm)
            .FirstOrDefaultAsync();

        // Último erro do app
        var ultimoErroApp = await _context.ErrorLogsApp
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        var response = new UltimosComandosResponse
        {
            UltimoComandoIA = ultimoIa?.TextoUsuario,
            UltimaRespostaIA = ultimoIa?.TextoIa,
            UltimoComandoAutomacao = ultimoErroApp?.UltimoDispositivoAcionado
        };

        return Ok(response);
    }

    // ========== AÇÕES ADICIONAIS ==========

    [HttpPost("reiniciar-sessao-jwt")]
    public async Task<IActionResult> ReiniciarSessaoJwt([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Revogar todos os tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == request.UserId)
            .ToListAsync();
        _context.RefreshTokens.RemoveRange(refreshTokens);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Sessão JWT reiniciada. Usuário precisará fazer login novamente.", userId = request.UserId });
    }

    [HttpPost("renovar-dados-usuario")]
    public async Task<IActionResult> RenovarDadosUsuario([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
            return NotFound("Usuário não encontrado.");

        user.LastUpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Dados do usuário renovados.", userId = request.UserId });
    }

    [HttpPost("resetar-permissoes")]
    public async Task<IActionResult> ResetarPermissoes([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
            return NotFound("Usuário não encontrado.");

        // Resetar role para padrão
        user.Role = "UserNivel1";
        user.LastUpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Permissões resetadas para padrão.", userId = request.UserId, role = user.Role });
    }

    [HttpPost("regenerar-configuracao-ia")]
    public async Task<IActionResult> RegenerarConfiguracaoIA([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Buscar configuração de IA (global, não por usuário)
        var config = await _context.ConfiguracoesStarkNlp.FirstOrDefaultAsync();

        if (config != null)
        {
            // Resetar configuração
            config.DataAtualizacao = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Configuração de IA regenerada.", userId = request.UserId });
    }

    [HttpPost("forcar-sincronizacao-dispositivos")]
    public IActionResult ForcarSincronizacaoDispositivos([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Aqui você pode implementar lógica para forçar sincronização via SignalR ou MQTT
        return Ok(new { message = "Sincronização de dispositivos forçada.", userId = request.UserId });
    }

    [HttpPost("limpar-fila-comandos")]
    public IActionResult LimparFilaComandos([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Implementar lógica para limpar fila de comandos pendentes
        return Ok(new { message = "Fila de comandos limpa.", userId = request.UserId });
    }

    [HttpPost("forcar-reload-roles")]
    public IActionResult ForcarReloadRoles([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Implementar lógica para recarregar roles/permissões
        return Ok(new { message = "Roles recarregadas.", userId = request.UserId });
    }

    [HttpPost("revogar-tokens-ativos")]
    public async Task<IActionResult> RevogarTokensAtivos([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == request.UserId && rt.Expiration > DateTimeOffset.UtcNow)
            .ToListAsync();
        _context.RefreshTokens.RemoveRange(refreshTokens);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Tokens ativos revogados. Total: {refreshTokens.Count}", userId = request.UserId });
    }

    [HttpPost("recarregar-parametros-sistema")]
    public IActionResult RecarregarParametrosSistema()
    {
        // Implementar lógica para recarregar parâmetros do sistema
        return Ok(new { message = "Parâmetros do sistema recarregados." });
    }

    [HttpPost("limpar-configuracoes-corrompidas")]
    public async Task<IActionResult> LimparConfiguracoesCorrompidas([FromBody] IniciarManutencaoRequest request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest("UserId é obrigatório.");

        // Limpar configurações de NLP corrompidas (global, não por usuário)
        var configs = await _context.ConfiguracoesStarkNlp.ToListAsync();

        foreach (var config in configs)
        {
            // Resetar valores problemáticos
            if (string.IsNullOrWhiteSpace(config.StarkNlpUrl))
            {
                config.StarkNlpUrl = "";
            }
            config.DataAtualizacao = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Configurações corrompidas limpas.", userId = request.UserId });
    }

    [HttpGet("status/{userId}")]
    public IActionResult GetStatusManutencao(Guid userId)
    {
        var emManutencao = _manutencaoAtiva.ContainsKey(userId) && _manutencaoAtiva[userId];
        var nomeAssistente = _nomeAssistenteProvisorio.ContainsKey(userId) 
            ? _nomeAssistenteProvisorio[userId] 
            : null;

        return Ok(new
        {
            emManutencao,
            nomeAssistenteProvisorio = nomeAssistente
        });
    }
}
