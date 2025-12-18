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
        var response = await _http.PostAsJsonAsync($"ewelink/{deviceId}/toggle", new { TurnOn = turnOn });
        return response.IsSuccessStatusCode;
    }
}
