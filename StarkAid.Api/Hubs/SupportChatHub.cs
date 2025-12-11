using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.Suporte;
using StarkAid.Api.Entities;
using StarkAid.Api.Services.V1.Suporte;
using System.Security.Claims;

namespace StarkAid.Api.Hubs;

[Authorize]
public class SupportChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly ISupportQueueService _queueService;
    private readonly ISupportIaService _iaService;
    private readonly ISuporteChatService _suporteChatService;
    private readonly IHubContext<DeviceHub> _deviceHubContext;
    private readonly IHubContext<DispositivoEspHub> _dispositivoEspHubContext;
    private readonly ILogger<SupportChatHub> _logger;
    private static readonly Dictionary<Guid, Guid> _conversasAtivas = new(); // userId -> conversaId

    public SupportChatHub(
        AppDbContext context,
        ISupportQueueService queueService,
        ISupportIaService iaService,
        ISuporteChatService suporteChatService,
        IHubContext<DeviceHub> deviceHubContext,
        IHubContext<DispositivoEspHub> dispositivoEspHubContext,
        ILogger<SupportChatHub> logger)
    {
        _context = context;
        _queueService = queueService;
        _iaService = iaService;
        _suporteChatService = suporteChatService;
        _deviceHubContext = deviceHubContext;
        _dispositivoEspHubContext = dispositivoEspHubContext;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         Context.User?.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            await Clients.Caller.SendAsync("Error", "Usuário não autenticado.");
            Context.Abort();
            return;
        }

        // Obter origem (app ou software) da query string ou do contexto
        var origem = Context.GetHttpContext()?.Request.Query["origem"].FirstOrDefault() ?? "software";

        // Adicionar à fila
        var posicao = await _queueService.AdicionarUsuario(userId, Context.ConnectionId, origem);

        // Adicionar ao grupo do usuário
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // Se for administrador, adicionar ao grupo de suporte
        var user = await _context.Users.FindAsync(userId);
        if (user != null && user.Role == "Administrador")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "support_staff");
        }

        // Verificar se está em atendimento
        var emAtendimento = await _queueService.UsuarioEmAtendimento(userId);
        
        // Notificar posição na fila
        if (posicao > 0 && !emAtendimento)
        {
            await Clients.Caller.SendAsync("QueuePosition", new { posicao, message = $"Aguarde, você está na fila. Posição: {posicao}" });
        }
        else if (emAtendimento || posicao == 0)
        {
            // Usuário é o próximo ou foi atendido imediatamente
            await Clients.Caller.SendAsync("NextInQueue", new { message = "Você é o próximo" });
            try
            {
                await IniciarAtendimento(userId, origem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar atendimento para usuário {UserId}", userId);
                await Clients.Caller.SendAsync("Error", "Erro ao iniciar atendimento. Por favor, tente novamente.");
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         Context.User?.FindFirstValue("sub");

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await _queueService.RemoverUsuario(userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string message)
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         Context.User?.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            await Clients.Caller.SendAsync("Error", "Usuário não autenticado.");
            return;
        }

        var origem = Context.GetHttpContext()?.Request.Query["origem"].FirstOrDefault() ?? "software";

        // Verificar se está em atendimento
        var emAtendimento = await _queueService.UsuarioEmAtendimento(userId);
        if (!emAtendimento)
        {
            await Clients.Caller.SendAsync("Error", "Você não está em atendimento. Aguarde sua vez na fila.");
            return;
        }

        // Verificar se conversa está bloqueada (limite atingido)
        Guid conversaId;
        if (_conversasAtivas.TryGetValue(userId, out conversaId))
        {
            var conversa = await _context.SuporteConversas.FindAsync(conversaId);
            if (conversa != null && conversa.LimiteAtingido)
            {
                await Clients.Caller.SendAsync("Error", "Limite de contexto atingido. Por favor, preencha o formulário de suporte.");
                return;
            }
        }

        // Obter ou criar conversa
        if (!_conversasAtivas.TryGetValue(userId, out conversaId))
        {
            // Primeira mensagem - processar inicial
            string resposta;
            try
            {
                _logger.LogInformation("Processando mensagem inicial para usuário {UserId}, origem: {Origem}, mensagem: {Mensagem}", userId, origem, message);
                resposta = await _suporteChatService.ProcessarMensagemInicial(userId, origem, message);
                _logger.LogInformation("Resposta gerada para usuário {UserId}: {Resposta}", userId, resposta?.Substring(0, Math.Min(100, resposta?.Length ?? 0)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem inicial para usuário {UserId}", userId);
                resposta = "Olá! 👋\n\nSou o assistente virtual de suporte da StarkAid. Como posso ajudá-lo hoje?";
            }
            
            // Obter conversa criada
            try
            {
                var conversa = await _context.SuporteConversas
                    .Where(c => c.UserId == userId && !c.ChatConcluido && c.Origem == origem)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();
                
                if (conversa != null)
                {
                    conversaId = conversa.Id;
                    _conversasAtivas[userId] = conversaId;
                    _logger.LogInformation("Conversa {ConversaId} associada ao usuário {UserId}", conversaId, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter conversa criada para usuário {UserId}", userId);
            }

            // Verificar se resposta contém comando
            var respostaFinal = await ProcessarComandoNaResposta(resposta ?? "Desculpe, não consegui processar sua mensagem. Por favor, tente novamente.", userId, origem);

            _logger.LogInformation("Enviando mensagem para usuário {UserId}: {Mensagem}", userId, respostaFinal?.Substring(0, Math.Min(100, respostaFinal?.Length ?? 0)));
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessageDto
            {
                Message = respostaFinal,
                Sender = "ia",
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Origem = origem
            });
        }
        else
        {
            // Mensagem subsequente - adicionar delay para efeito visual
            await Task.Delay(500); // Pequeno delay para dar tempo da mensagem do usuário aparecer
            
            string resposta;
            try
            {
                _logger.LogInformation("Processando mensagem subsequente para usuário {UserId}, conversa: {ConversaId}, mensagem: {Mensagem}", userId, conversaId, message);
                resposta = await _suporteChatService.ProcessarMensagemUsuario(userId, origem, message, conversaId);
                _logger.LogInformation("Resposta gerada para usuário {UserId}: {Resposta}", userId, resposta?.Substring(0, Math.Min(100, resposta?.Length ?? 0)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem do usuário {UserId}", userId);
                resposta = "Desculpe, ocorreu um erro ao processar sua mensagem. Por favor, tente novamente.";
            }
            
            // Adicionar delay adicional para simular processamento
            await Task.Delay(800);
            
            // Verificar se resposta contém comando
            var respostaFinal = await ProcessarComandoNaResposta(resposta ?? "Desculpe, não consegui processar sua mensagem. Por favor, tente novamente.", userId, origem);

            // Verificar se limite foi atingido
            try
            {
                var conversa = await _context.SuporteConversas.FindAsync(conversaId);
                if (conversa != null && conversa.LimiteAtingido)
                {
                    await Clients.Caller.SendAsync("LimiteAtingido", "Limite de contexto atingido.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar limite para usuário {UserId}", userId);
            }

            _logger.LogInformation("Enviando mensagem para usuário {UserId}: {Mensagem}", userId, respostaFinal?.Substring(0, Math.Min(100, respostaFinal?.Length ?? 0)));
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessageDto
            {
                Message = respostaFinal,
                Sender = "ia",
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Origem = origem
            });
        }
    }

    private async Task<string> ProcessarComandoNaResposta(string resposta, Guid userId, string origem)
    {
        // Verificar se resposta contém [COMANDO:...]
        var comandoMatch = System.Text.RegularExpressions.Regex.Match(resposta, @"\[COMANDO:([^\]]+)\]");
        if (comandoMatch.Success)
        {
            var comando = comandoMatch.Groups[1].Value;
            var comandoCompleto = origem == "software" ? $"suporteToSoft:{comando}" : $"suporteToApp:{comando}";

            // Remover [COMANDO:...] da resposta antes de enviar ao usuário
            var respostaSemComando = System.Text.RegularExpressions.Regex.Replace(resposta, @"\[COMANDO:[^\]]+\]", "").Trim();
            
            // Se há texto antes do comando, enviar primeiro
            if (!string.IsNullOrEmpty(respostaSemComando))
            {
                await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessageDto
                {
                    Message = respostaSemComando,
                    Sender = "ia",
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    Origem = origem
                });
            }
            
            // Enviar mensagem de processamento
            var acaoNome = comando switch
            {
                "limparcache" => "Limpando cache",
                "atualizardados" => "Atualizando dados",
                "logout" => "Desconectando",
                "limpardados" => "Limpando dados",
                _ => "Processando"
            };
            
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessageDto
            {
                Message = $"⏳ {acaoNome}...",
                Sender = "ia",
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Origem = origem
            });

            // Enviar comando via SignalR
            if (origem == "software")
            {
                await _dispositivoEspHubContext.Clients.Group("type_software").SendAsync("SuporteComando", comandoCompleto);
            }
            else
            {
                await _deviceHubContext.Clients.Group(userId.ToString()).SendAsync("SuporteComando", comandoCompleto);
            }

            // Salvar ação
            var acao = new StarkAid.Api.Entities.SuporteAcao
            {
                UserId = userId,
                Origem = origem,
                Acao = comando,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.SuporteAcoes.Add(acao);
            await _context.SaveChangesAsync();

            // Aguardar alguns segundos para simular processamento
            await Task.Delay(2500);
            
            // Enviar confirmação
            var acaoNomeCompleto = comando switch
            {
                "limparcache" => "limpeza de cache",
                "atualizardados" => "atualização de dados",
                "logout" => "logout",
                "limpardados" => "limpeza de dados",
                _ => comando
            };
            
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessageDto
            {
                Message = $"✅ {acaoNomeCompleto} concluída!\n\nPor favor, verifique se o problema foi resolvido. Se ainda não estiver funcionando, me avise e vou tentar outra solução.",
                Sender = "ia",
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Origem = origem
            });
            
            // Retornar string vazia para não enviar mensagem duplicada
            return "";
        }

        return resposta;
    }

    // Método para receber resposta de ação do cliente
    public async Task AcaoExecutada(string acao, bool sucesso, string? resposta = null)
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         Context.User?.FindFirstValue("sub");

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            var origem = Context.GetHttpContext()?.Request.Query["origem"].FirstOrDefault() ?? "software";

            // Atualizar ação
            var acaoEntity = await _context.SuporteAcoes
                .Where(a => a.UserId == userId && a.Acao == acao && a.Origem == origem)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (acaoEntity != null)
            {
                acaoEntity.Sucesso = sucesso;
                acaoEntity.Resposta = resposta;
                await _context.SaveChangesAsync();
            }

            // Não enviar mensagem duplicada - a confirmação já foi enviada em ProcessarComandoNaResposta
            // Apenas atualizar o status da ação no banco
            _logger.LogInformation("Ação {Acao} executada pelo usuário {UserId} com sucesso: {Sucesso}", acao, userId, sucesso);
        }
    }

    private async Task IniciarAtendimento(Guid userId, string origem)
    {
        // Obter informações do usuário
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        // Verificar se já existe conversa ativa
        SuporteConversa? conversaExistente = null;
        try
        {
            conversaExistente = await _context.SuporteConversas
                .Where(c => c.UserId == userId && !c.ChatConcluido && c.Origem == origem)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208) // Invalid object name
        {
            _logger.LogError(sqlEx, "Tabela SuporteConversas não existe. A migration precisa ser aplicada.");
            // Continuar sem conversa existente
        }

        if (conversaExistente != null)
        {
            _conversasAtivas[userId] = conversaExistente.Id;
        }

        // Capturar logs automaticamente
        List<object> logs;
        if (origem == "software")
        {
            logs = (await _context.ErrorLogsSoft
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync()).Cast<object>().ToList();
        }
        else
        {
            logs = (await _context.ErrorLogsApp
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync()).Cast<object>().ToList();
        }

        // Mensagem de saudação inicial
        try
        {
            var saudacao = await _iaService.GerarSaudacaoInicial(userId, user.Name, user.Email, origem, logs);
            
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessageDto
            {
                Message = saudacao,
                Sender = "ia",
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Origem = origem
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar saudação inicial");
            // Enviar mensagem padrão se houver erro
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessageDto
            {
                Message = $"Olá {user.Name}! 👋\n\nSou o assistente virtual de suporte da StarkAid. Como posso ajudá-lo hoje?",
                Sender = "ia",
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Origem = origem
            });
        }
    }

    // Método para atendentes humanos
    [Authorize(Policy = "AdministradorOnly")]
    public async Task SendSupportMessage(Guid userId, string message)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveMessage", new ChatMessageDto
        {
            Message = message,
            Sender = "support",
            Timestamp = DateTime.UtcNow,
            UserId = userId
        });
    }

    // Método para transferir para suporte humano
    public async Task TransferToHumanSupport()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         Context.User?.FindFirstValue("sub");

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await _queueService.MarcarParaTransferencia(userId);
            await Clients.Group("support_staff").SendAsync("TransferRequest", new { userId });
            await Clients.Caller.SendAsync("TransferInitiated", new { message = "Sua solicitação foi transferida para suporte humano. Aguarde..." });
        }
    }
}
