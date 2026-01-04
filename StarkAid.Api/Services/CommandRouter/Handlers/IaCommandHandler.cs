using System;
using System.Threading.Tasks;
using StarkAid.Api.DTOs.Commands;
using StarkAid.Api.Services.V1.SuperIA;

namespace StarkAid.Api.Services.CommandRouter.Handlers
{
    public sealed class IaCommandHandler : ICommandHandler
    {
        private readonly IaService _iaService;

        public IaCommandHandler(IaService iaService)
        {
            _iaService = iaService;
        }

        // Este handler é o fallback final, então ele "pode" lidar com qualquer coisa que chegue a ele
        public bool CanHandle(CommandRequestDto request)
            => true;

        public async Task<CommandResult> ExecuteAsync(CommandRequestDto request)
        {
            try
            {
                // Aqui usaríamos a lógica de processamento de IA existente
                // Exemplo simplificado:
                var mensagens = new[]
                {
                    new { role = "system", content = "Você é o assistente virtual StarkAid." },
                    new { role = "user", content = request.Texto }
                };

                var resultado = await _iaService.ProcessarMensagemJson(mensagens);

                if (resultado != null && !string.IsNullOrWhiteSpace(resultado.Texto))
                {
                    return CommandResult.Success(resultado.Texto);
                }
            }
            catch (Exception ex)
            {
                return CommandResult.Fail($"Erro ao processar IA: {ex.Message}");
            }

            return CommandResult.Fail("A IA não conseguiu gerar uma resposta adequada.");
        }
    }
}
