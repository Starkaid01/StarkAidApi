using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace StarkAid.Api.Hubs;

/// <summary>
/// Hub para comunicação entre API, Software Windows Forms, App Android e Dispositivos ESP
/// </summary>
public class DispositivoEspHub : Hub
{
    private readonly ILogger<DispositivoEspHub> _logger;

    public DispositivoEspHub(ILogger<DispositivoEspHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var connectionType = httpContext?.Request.Query["type"].FirstOrDefault() ?? "unknown";
        
        // Se o tipo estiver vazio, tenta usar "software" como padrão para compatibilidade
        if (string.IsNullOrWhiteSpace(connectionType) || connectionType == "unknown")
        {
            connectionType = "software";
        }
        
        var clientId = Context.ConnectionId;

        _logger.LogInformation("Cliente conectado: {ConnectionId}, Tipo: {Type}", clientId, connectionType);

        // Adiciona a grupos baseado no tipo
        var groupName = $"type_{connectionType}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Groups.AddToGroupAsync(Context.ConnectionId, "all_clients");

        _logger.LogInformation("Cliente {ConnectionId} adicionado ao grupo {GroupName}", clientId, groupName);

        await Clients.Caller.SendAsync("Connected", new { connectionId = clientId, type = connectionType });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_clients");
        await base.OnDisconnectedAsync(exception);
    }

    // Recebe comando do App/Software e envia para Software Windows Forms
    public async Task EnviarComandoParaSoftware(string nome, string ip, int porta, string comando, string? comandToEsp = null)
    {
        try
        {
            _logger.LogInformation("Comando recebido via Hub: Nome={Nome}, IP={Ip}, Porta={Porta}, Comando={Comando}, ComandToEsp={ComandToEsp}", 
                nome, ip, porta, comando, comandToEsp ?? comando);
            
            var comandoData = new
            {
                nome,
                ip,
                porta,
                comando,
                comandToEsp = comandToEsp ?? comando
            };
            
            // Envia para grupo de software Windows Forms
            _logger.LogInformation("Enviando comando para grupo 'type_software' via Hub");
            await Clients.Group("type_software").SendAsync("ComandoDispositivo", comandoData);
            _logger.LogInformation("Comando enviado com sucesso para grupo 'type_software'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar comando via Hub: {Message}", ex.Message);
            throw;
        }
    }

    // Recebe resposta do Software Windows Forms e retorna para App
    public async Task ReceberRespostaDoSoftware(string nome, string ip, int porta, string resposta)
    {
        _logger.LogInformation("Resposta recebida: {Nome} - {Ip}:{Porta} - {Resposta}", nome, ip, porta, resposta);
        
        // Envia para todos os clientes (App, API, etc)
        await Clients.All.SendAsync("RespostaDispositivo", new
        {
            nome,
            ip,
            porta,
            resposta
        });
    }

    // Atualiza status de dispositivo
    public async Task AtualizarStatusDispositivo(string nome, string ip, string status, bool ligadoDesligado)
    {
        _logger.LogInformation("Status atualizado: {Nome} - {Ip} - {Status}", nome, ip, status);
        
        await Clients.All.SendAsync("StatusDispositivoAtualizado", new
        {
            nome,
            ip,
            status,
            ligadoDesligado
        });
    }

    // Envia logs de erro
    public async Task EnviarLogErro(string origem, string mensagem, string? detalhes = null)
    {
        _logger.LogWarning("Log de erro recebido de {Origem}: {Mensagem}", origem, mensagem);
        
        await Clients.Group("type_api").SendAsync("LogErro", new
        {
            origem,
            mensagem,
            detalhes,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    // Envia dados de uso do app
    public async Task EnviarDadosUso(string origem, object dados)
    {
        _logger.LogInformation("Dados de uso recebidos de {Origem}", origem);
        
        await Clients.Group("type_api").SendAsync("DadosUso", new
        {
            origem,
            dados,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    // Identifica o tipo de cliente
    public async Task IdentificarCliente(string tipo, string? identificador = null)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "type_unknown");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"type_{tipo}");
        
        if (!string.IsNullOrEmpty(identificador))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"client_{identificador}");
        }

        await Clients.Caller.SendAsync("Identificado", new { tipo, identificador });
    }

    // Envia mensagem "ToSoft" para o software Windows Forms
    public async Task EnviarMensagemToSoft(string mensagem)
    {
        _logger.LogInformation("Enviando mensagem ToSoft para software: {Mensagem}", mensagem);
        
        // Envia diretamente para o grupo de software Windows Forms
        await Clients.Group("type_software").SendAsync("ToSoft", mensagem);
        
        _logger.LogInformation("Mensagem ToSoft enviada com sucesso para grupo 'type_software'");
    }
}

