using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public interface IHealthCheckService
{
    Task<HealthStatusDto?> GetStatusAsync();
}

public class HealthCheckService : IHealthCheckService
{
    private readonly HttpClient _http;

    public HealthCheckService(HttpClient http)
    {
        _http = http;
    }

    public async Task<HealthStatusDto?> GetStatusAsync()
    {
        return await _http.GetFromJsonAsync<HealthStatusDto>("health");
    }
}

public record HealthStatusDto(bool IsHealthy, string Message);
