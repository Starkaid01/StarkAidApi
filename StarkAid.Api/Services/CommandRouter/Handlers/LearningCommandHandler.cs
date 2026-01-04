using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;
using StarkAid.Api.Services.V1.IA;

namespace StarkAid.Api.Services.CommandRouter.Handlers
{
    public sealed class LearningCommandHandler : ICommandHandler
    {
        private readonly IAprendizadoService _aprendizadoService;

        public LearningCommandHandler(IAprendizadoService aprendizadoService)
        {
            _aprendizadoService = aprendizadoService;
        }

        public bool CanHandle(CommandRequestDto request)
            => request.Contexto == "privado" || request.Contexto == "global" || request.Contexto == "followup";

        public async Task<CommandResult> ExecuteAsync(CommandRequestDto request)
        {
            // O contexto do DTO pode ser mapeado para o contexto de ancoragem do Aprendizado
            var resposta = await _aprendizadoService.BuscarAprendizadoAsync(request.UserId, request.Texto, request.Contexto);

            if (!string.IsNullOrEmpty(resposta.Resposta))
                return CommandResult.Success(resposta.Resposta);

            return CommandResult.Fail("Nenhum aprendizado correspondente.");
        }
    }
}
