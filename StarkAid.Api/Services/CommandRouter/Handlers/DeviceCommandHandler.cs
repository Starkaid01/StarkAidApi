using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;

namespace StarkAid.Api.Services.CommandRouter.Handlers
{
    public sealed class DeviceCommandHandler : ICommandHandler
    {
        public bool CanHandle(CommandRequestDto request)
            => !string.IsNullOrEmpty(request.DeviceId);

        public async Task<CommandResult> ExecuteAsync(CommandRequestDto request)
        {
            // Placeholder: Lógica real de validação de ownership e envio MQTT/SignalR viria aqui
            // Exemplo: _deviceService.SendCommand(request.DeviceId, request.Texto);
            
            // Simulando sucesso (assíncrono)
            await Task.CompletedTask;
            
            return CommandResult.Success($"Comando enviado ao dispositivo {request.DeviceId}: {request.Texto}");
        }
    }
}
