using System;
using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;
using StarkAid.Api.Services.V1.Fun;

namespace StarkAid.Api.Services.CommandRouter.Handlers
{
    public class FunCommandHandler : ICommandHandler
    {
        private readonly IIntentDetector _intentDetector;
        private readonly ILocalCommandRouter _funRouter;

        public FunCommandHandler(IIntentDetector intentDetector, ILocalCommandRouter funRouter)
        {
            _intentDetector = intentDetector;
            _funRouter = funRouter;
        }

        public bool CanHandle(CommandRequestDto request)
        {
            // Use IntentDetector to check if this is a Fun command
            var intent = _intentDetector.DetectIntent(request.Texto);
            return intent != FunIntent.None;
        }

        public async Task<CommandResult> ExecuteAsync(CommandRequestDto request)
        {
            var result = await _funRouter.TryExecuteFunCommandAsync(request.UserId, request.Texto);
            
            if (result.Handled)
            {
                return CommandResult.Success(result.Response);
            }

            return CommandResult.Fail("Não foi possível processar o comando divertido.");
        }
    }
}
