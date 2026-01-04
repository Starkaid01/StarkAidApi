using System;
using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;

namespace StarkAid.Api.Services.CommandRouter.Handlers
{
    public sealed class SystemCommandHandler : ICommandHandler
    {
        public bool CanHandle(CommandRequestDto request)
        {
            var text = request.Texto.ToLower();
            return text.Contains("parar de ouvir") || 
                   text.Contains("ajuda") || 
                   text.Contains("versão");
        }

        public async Task<CommandResult> ExecuteAsync(CommandRequestDto request)
        {
            var text = request.Texto.ToLower();

            if (text.Contains("parar de ouvir"))
                return CommandResult.Success("Ok, parando de ouvir.");
                
            if (text.Contains("ajuda"))
                return CommandResult.Success("Eu sou o StarkAid. Posso controlar seus dispositivos IoT e conversar com você.");

            if (text.Contains("versão"))
                return CommandResult.Success("StarkAid API v1.0.0");

            return CommandResult.Fail("Comando de sistema não reconhecido.");
        }
    }
}
