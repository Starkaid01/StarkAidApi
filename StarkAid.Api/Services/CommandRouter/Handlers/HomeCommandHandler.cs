using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;
using StarkAid.Api.Services.V1.Comodos;

namespace StarkAid.Api.Services.CommandRouter.Handlers
{
    public sealed class HomeCommandHandler : ICommandHandler
    {
        private readonly IComodoService _comodoService;

        public HomeCommandHandler(IComodoService comodoService)
        {
            _comodoService = comodoService;
        }

        public bool CanHandle(CommandRequestDto request)
        {
            var text = request.Texto.ToLower();
            return text.Contains("apagar tudo") || 
                   text.Contains("desligar tudo") || 
                   text.Contains("apaga tudo") ||
                   text.Contains("desliga tudo") ||
                   text.Contains("encerrar o dia") ||
                   text.Contains("apagar as luzes") ||
                   text.Contains("desligar as luzes") ||
                   text.Contains("ligar tudo") ||
                   text.Contains("liga tudo");
        }

        public async Task<CommandResult> ExecuteAsync(CommandRequestDto request)
        {
            var text = request.Texto.ToLower();
            bool turnOn = text.Contains("ligar tudo") || text.Contains("liga tudo");

            var result = await _comodoService.ControlAllDevicesAsync(request.UserId, turnOn);
            
            return CommandResult.Success(result);
        }
    }
}
