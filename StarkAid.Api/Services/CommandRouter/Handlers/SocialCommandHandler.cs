using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;
using StarkAid.Api.Services.V1.SocialCommand;

namespace StarkAid.Api.Services.CommandRouter.Handlers
{
    public sealed class SocialCommandHandler : ICommandHandler
    {
        private readonly ComandoSocialService _socialService;

        public SocialCommandHandler(ComandoSocialService socialService)
        {
            _socialService = socialService;
        }

        public bool CanHandle(CommandRequestDto request)
            => request.Contexto == "privado" || request.Contexto == "global";

        public async Task<CommandResult> ExecuteAsync(CommandRequestDto request)
        {
            var resposta = await _socialService.ProcessSocialAsync(request.UserId, request.Texto);
            
            if (resposta != null)
                return CommandResult.Success(resposta);

            return CommandResult.Fail("Nenhum comando social correspondente.");
        }
    }
}
