using StarkAid.Api.DTOs.Suporte;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.V1.Suporte;

public interface ISuporteChatService
{
    Task<string> ProcessarMensagemInicial(Guid userId, string origem, string mensagem);
    Task<string> ProcessarMensagemUsuario(Guid userId, string origem, string mensagem, Guid conversaId);
    Task<List<ConsultaErroResponse>> ConsultarErrosDoUsuario(Guid userId, string origem);
    Task<List<ConsultaErroResponse>> FiltrarSolucoesViaveis(List<ConsultaErroResponse> consultas);
    Task<bool> VerificarSeResolvido(string mensagem);
    Task SalvarAprendizado(Guid userId, string origem, string problema, List<string> solucoesQueFuncionaram);
    Task<List<SuporteAprendizado>> BuscarAprendizadoSimilar(string problema, string origem);
    Task<bool> VerificarLimiteMensagens(Guid conversaId);
    Task MarcarConversaConcluida(Guid conversaId, bool resolvido);
}

public class ConsultaErroResponse
{
    public string CodigoErro { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public List<string> Solucoes { get; set; } = new();
}
