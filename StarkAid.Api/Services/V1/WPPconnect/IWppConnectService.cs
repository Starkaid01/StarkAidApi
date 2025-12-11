using System.Threading.Tasks;

namespace StarkAid.Api.Services.V1.WPPconnect;

/// <summary>
/// Interface mínima para o WPPConnect. Por enquanto contém apenas um método de “ping”.
/// </summary>
public interface IWppConnectService
{
    Task<bool> PingAsync(string sessionName);
}
