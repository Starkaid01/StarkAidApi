using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;

namespace StarkAid.Api.Services.CommandRouter
{
    public sealed class CommandRouter : ICommandRouter
    {
        private readonly IEnumerable<ICommandHandler> _handlers;

        public CommandRouter(IEnumerable<ICommandHandler> handlers)
        {
            _handlers = handlers;
        }

        public async Task<CommandResult> RouteAsync(CommandRequestDto request)
        {
            var matchingHandlers = _handlers.Where(h => h.CanHandle(request)).ToList();
            System.Console.WriteLine($"[CommandRouter] Processando: '{request.Texto}'");

            foreach (var handler in matchingHandlers)
            {
                System.Console.WriteLine($"[CommandRouter] Tentando: {handler.GetType().Name}");
                var result = await handler.ExecuteAsync(request);
                if (result.IsSuccess)
                {
                    System.Console.WriteLine($"[CommandRouter] Sucesso: {handler.GetType().Name}");
                    return result;
                }
            }

            return CommandResult.Fail("Comando não reconhecido por nenhum manipulador local.");
        }
    }
}
