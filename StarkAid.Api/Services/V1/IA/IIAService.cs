using System.Threading.Tasks;
using StarkAid.Api.DTOs.SuperIA;

namespace StarkAid.Api.Services.V1.IA
{
    public interface IIAService
    {
        Task<IaResultado?> ProcessMessageAsync(string userContext,
                                               string iaContext,
                                               string texto,
                                               string estilo);

        Task<IaResultado?> ProcessMessageWppAsync(string userContext,
                                                  string iaContext,
                                                  string texto,
                                                  string estilo);

        Task<IaResultado?> ProcessMessageJsonAsync(object[] mensagens);

        decimal CalculateCostUsd(IaResultado resultado);
        Task<string> SummarizeAsync(string texto, string estilo);
        Task<string?> GenerateAlternativeResponsesAsync(string respostaOriginal,
                                                        string estilo);

        Task<IaResultado?> ChamarStarkNlp(string texto);
        Task<IaResultado?> ChamarOpenRouter(object[] mensagens);
        Task<IaResultado?> ProcessarMensagemJson(object[] mensagens);
        Task<IaResultado?> ProcessarMensagemWpp(string userContext,
                                                string iaContext,
                                                string texto,
                                                string estilo);
        decimal CalcularCustoUSD(IaResultado resultado);
    }
}