using StarkAid.Web.DTOs;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public interface ILicenseService
{
    Task<LicenseDto?> GetLicenseAsync();
}

public class LicenseService : ILicenseService
{
    private readonly HttpClient _http;

    public LicenseService(HttpClient http)
    {
        _http = http;
    }

    public async Task<LicenseDto?> GetLicenseAsync()
    {
        return await _http.GetFromJsonAsync<LicenseDto>("license");
    }
}
