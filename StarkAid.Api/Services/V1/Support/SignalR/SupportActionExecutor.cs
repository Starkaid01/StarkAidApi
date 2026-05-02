using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using StarkAid.Api.Data;
using StarkAid.Api.Hubs;
using Microsoft.Extensions.Logging;

namespace StarkAid.Api.Services.V1.Support.SignalR;

public interface ISupportActionExecutor
{
    Task<string> ExecuteActionAsync(Guid userId, string actionName, string origem);
}

public class SupportActionExecutor : ISupportActionExecutor
{
    private readonly IHubContext<DeviceHub> _deviceHubContext;
    private readonly IHubContext<DispositivoEspHub> _dispositivoEspHubContext;
    private readonly ILogger<SupportActionExecutor> _logger;

    public SupportActionExecutor(
        IHubContext<DeviceHub> deviceHubContext,
        IHubContext<DispositivoEspHub> dispositivoEspHubContext,
        ILogger<SupportActionExecutor> logger)
    {
        _deviceHubContext = deviceHubContext;
        _dispositivoEspHubContext = dispositivoEspHubContext;
        _logger = logger;
    }

    public async Task<string> ExecuteActionAsync(Guid userId, string actionName, string origem)
    {
        _logger.LogInformation("Executando ação de suporte: {Action} para usuário {UserId} (Origem: {Origem})", actionName, userId, origem);

        string comando = actionName switch
        {
            "CleanAppCache" => "clean",
            "CleanLocalDatabase" => "clean-data-base",
            "CleanAppData" => "clean-dados",
            "RestartApp" => "restart",
            "Logout" => "logout",
            _ when actionName.StartsWith("UpdateDeviceName") => "update-device-name",
            _ => actionName // Se já vier o comando interno
        };

        var comandoCompleto = origem == "software" ? $"suporteToSoft:{comando}" : $"suporteToApp:{comando}";
        
        // Se for UpdateDeviceName, anexar parâmetros
        if (actionName.StartsWith("UpdateDeviceName"))
        {
            var parts = actionName.Split(':');
            if (parts.Length == 3)
            {
                comandoCompleto += $":{parts[1]}:{parts[2]}";
            }
        }

        try
        {
            if (origem == "software")
            {
                // Para software, geralmente usamos grupos por tipo ou talvez por userId se implementado
                await _dispositivoEspHubContext.Clients.Group("type_software").SendAsync("SuporteComando", comandoCompleto);
            }
            else
            {
                // Para App Android, usamos o grupo do userId
                await _deviceHubContext.Clients.Group(userId.ToString()).SendAsync("SuporteComando", comandoCompleto);
            }

            return $"Comando '{comando}' enviado com sucesso ao seu dispositivo.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar comando de suporte via SignalR");
            return "Erro ao tentar enviar o comando ao dispositivo. Verifique se ele está conectado.";
        }
    }
}
