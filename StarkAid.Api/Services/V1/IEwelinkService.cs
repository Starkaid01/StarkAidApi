using StarkAid.Api.DTOs.Ewelink;
using StarkAid.Api.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StarkAid.Api.Services.V1
{
    public interface IEwelinkService
    {
        Task<object> TrocarCodePorTokenAsync(string code, string region = "as");
        Task<object> RefreshTokenAsync(string refreshToken);
        Task<object> LoginDiretoAsync(string email, string password, string areaCode = "+55");
        Task<object> ListarFamiliasAsync(string accessToken, string region = "as");
        Task<object> ListarDispositivosAsync(string accessToken, string familyId, string region = "as");
        Task<object> ControlarDispositivoAsync(string accessToken, string deviceId, object parameters, string region = "as");
        
        // Métodos para trabalhar com banco de dados
        Task<EwelinkAccount> SaveOrUpdateAccountAsync(Guid userId, string accessToken, string refreshToken, long accessTokenExpiry, long refreshTokenExpiry, string? region = null);
        Task<EwelinkAccount?> GetAccountByUserIdAsync(Guid userId);
        Task<List<Entities.EwelinkDevice>> SaveOrUpdateDevicesAsync(Guid userId, List<Entities.EwelinkDevice> devices);
        Task<List<EwelinkDeviceResponse>> GetUserDevicesAsync(Guid userId);
        Task<EwelinkDeviceResponse?> GetDeviceStatusAsync(Guid userId, string deviceId);
        Task<bool> ControlDeviceAsync(Guid userId, string deviceId, bool switchOn);
        Task<bool> RefreshAccountTokenIfNeededAsync(Guid userId);
    }
}