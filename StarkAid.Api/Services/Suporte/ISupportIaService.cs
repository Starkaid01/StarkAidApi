namespace StarkAid.Api.Services.Suporte;

public interface ISupportIaService
{
    Task<string> GerarSaudacaoInicial(Guid userId, string nome, string email, string origem, object logs);
    Task<string> ProcessarMensagem(Guid userId, string mensagem, string origem);
    Task<List<string>> FiltrarSolucoes(List<string> solucoes, string origem);
}
