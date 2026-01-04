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

            foreach (var handler in matchingHandlers)
            {
                var result = await handler.ExecuteAsync(request);
                if (result.IsSuccess)
                {
                    return result;
                }
            }

            return CommandResult.Fail("Comando não reconhecido por nenhum manipulador local.");
        }
    }
}
