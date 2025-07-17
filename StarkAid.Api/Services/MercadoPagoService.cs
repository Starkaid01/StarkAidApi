using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarkAid.Api.DTOs;
using System.Net.Http.Headers;
using System.Text;

namespace StarkAid.Api.Services;

public class MercadoPagoService
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;

    public MercadoPagoService(IConfiguration config)
    {
        _httpClient = new HttpClient();
        _accessToken = config["MercadoPago:AccessToken"];
    }

    public async Task<AssinaturaResponseDto> CriarAssinaturaAsync(string email, decimal valor, Guid userId)
    {
        var request = new
        {
            payer_email = email,
            back_url = "https://starkaid.com.br/assinatura/obrigado",
            reason = "Assinatura Mensal StarkAid",
            external_reference = userId.ToString(),
            auto_recurring = new
            {
                frequency = 1,
                frequency_type = "months",
                transaction_amount = valor,
                currency_id = "BRL",
                start_date = DateTime.UtcNow.ToString("o")
            }
        };

        Console.WriteLine(JsonConvert.SerializeObject(request, Formatting.Indented));
        var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);


        var response = await _httpClient.PostAsync("https://api.mercadopago.com/preapproval", content);

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var responseJson = JObject.Parse(responseBody);

        return new AssinaturaResponseDto
        {
            InitPoint = responseJson["init_point"].ToString(),
            PreapprovalId = responseJson["id"].ToString()
        };
    }

    public async Task<string> ConsultarAssinaturaStatusAsync(string preapprovalId)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        var response = await _httpClient.GetAsync($"https://api.mercadopago.com/preapproval/{preapprovalId}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(content);
        return json["status"]?.ToString();
    }
}
