using System.Net.Http;
using System.Threading.Tasks;

namespace StarkAid.Api.Services.V1.WPPconnect;

/// <summary>
/// Implementação “stub” do serviço de integração ao WPPConnect.
/// Basta manter o contrato da interface para que o código compile.
/// No futuro você pode preencher os métodos com chamadas reais ao WPPConnect.
/// </summary>
public class WppConnectService : IWppConnectService
{
    private readonly HttpClient _http;

    public WppConnectService(IHttpClientFactory httpFactory)
    {
        _http = httpFactory.CreateClient();
    }

    public async Task<bool> PingAsync(string sessionName)
    {
        // Exemplo simples – você pode adaptar ao endpoint real do WPPConnect.
        var response = await _http.GetAsync($"https://api.wppconnect.com/ping/{sessionName}");
        return response.IsSuccessStatusCode;
    }
}
