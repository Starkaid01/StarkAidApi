using StarkAid.Api.DTOs;

namespace StarkAid.Api.Services.V1.Suporte;

public interface ISupportIaService
{
    Task<string> GerarSaudacaoInicial(Guid userId, string nome, string email, string origem, object logs);
    Task<string> ProcessarMensagem(Guid userId, string mensagem, string origem, Guid? conversaId = null);
    Task<string> ProcessarMensagemComContexto(Guid userId, string mensagem, string origem, Guid conversaId);
    Task<List<string>> FiltrarSolucoes(List<string> solucoes, string origem);

    Task<StarkAid.Api.DTOs.EconomicPayload?> ObterEconomiaAsync(Guid userId);
}
