using StarkAid.Web.Dtos;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public interface IUserService
{
    Task<UserDto?> GetCurrentUserAsync();
}

public class UserService : IUserService
{
    private readonly HttpClient _http;

    public UserService(HttpClient http)
    {
        _http = http;
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        return await _http.GetFromJsonAsync<UserDto>("users/me");
    }
}

