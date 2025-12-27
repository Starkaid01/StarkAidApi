using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public interface IEwelinkService
{
    Task<bool> ToggleDeviceAsync(string deviceId, bool turnOn);
}

public class EwelinkService : IEwelinkService
{
    private readonly HttpClient _http;

    public EwelinkService(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> ToggleDeviceAsync(string deviceId, bool turnOn)
    {
        var payload = new { @switch = turnOn ? "on" : "off" };

        var response = await _http.PostAsJsonAsync(
            $"api/v1/ewelink/dispositivos/{deviceId}/controlar",
            payload);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        // Opcional: log do erro para debug
        var error = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Erro ao controlar dispositivo: {response.StatusCode} - {error}");
        return false;
    }
}
