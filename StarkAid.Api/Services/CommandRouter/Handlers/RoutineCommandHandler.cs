using System;
using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;
using StarkAid.Api.Services.V1.Rotinas;
using StarkAid.Api.Services.CommandRouter;
using Microsoft.Extensions.DependencyInjection;

namespace StarkAid.Api.Services.CommandRouter.Handlers
{
    public sealed class RoutineCommandHandler : ICommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public RoutineCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool CanHandle(CommandRequestDto request) => true; // Todos os comandos de texto podem ser gatilhos

        public async Task<CommandResult> ExecuteAsync(CommandRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Texto)) return CommandResult.Fail("Texto vazio");

            using var scope = _serviceProvider.CreateScope();
            var rotinaService = scope.ServiceProvider.GetRequiredService<IRotinaService>();

            // Verifica se este comando dispara alguma rotina (Ex: "boa noite")
            // Retorna sucesso se disparou pelo menos uma.
            // Para ser robusto, poderíamos retornar sucesso apenas se o texto for EXATAMENTE a expressão do gatilho
            // Mas o ProcessarGatilhosComandoAsync já faz o match.
            
            var triggered = await rotinaService.ProcessarGatilhosComandoAsync(request.UserId, request.Texto, request.ExecutionDepth);
            
            if (triggered)
            {
                return CommandResult.Success("");
            }

            return CommandResult.Fail("Continuar processando pipeline..."); 
        }
    }
}
