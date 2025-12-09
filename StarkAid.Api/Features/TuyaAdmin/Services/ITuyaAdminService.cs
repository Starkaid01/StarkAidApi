using StarkAid.Api.Features.TuyaAdmin.Models;

namespace StarkAid.Api.Features.TuyaAdmin.Services
{
    public interface ITuyaAdminService
    {
        Task<TuyaUserDto?> GetUserByEmailAsync(string email, CancellationToken ct = default);
        Task<TuyaUserDto?> CreateUserInCloudProjectAsync(string email, string password, CancellationToken ct = default);

        Task<bool> DeleteUserByUidAsync(string uid, CancellationToken ct = default);
        Task<IEnumerable<(string email, bool deleted, string message)>> CleanDuplicatesAsync(IEnumerable<string> emails, CancellationToken ct = default);
    }
}
