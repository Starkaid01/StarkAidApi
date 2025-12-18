using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using StarkAid.Web.DTOs;

public interface IDeviceService
{
    Task<List<DeviceDto>> GetDevicesAsync();
    Task<DeviceDto?> GetDeviceByIdAsync(Guid id);
}

public class DeviceService : IDeviceService
{
    private readonly HttpClient _http;

    public DeviceService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<DeviceDto>> GetDevicesAsync()
    {
        return await _http.GetFromJsonAsync<List<DeviceDto>>("devices") ?? new List<DeviceDto>();
    }

    public async Task<DeviceDto?> GetDeviceByIdAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<DeviceDto>($"devices/{id}");
    }
}
