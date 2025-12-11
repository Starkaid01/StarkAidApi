using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StarkAid.Api.Services.V1.Firebase
{
    public interface IFirebaseTokenService
    {
        Task SaveOrUpdateAsync(Guid userId, string token);
        Task<IReadOnlyCollection<string>> GetTokensAsync(Guid userId);
        Task DeleteAsync(Guid userId, string token);

        // ------------------- Compatibilidade -------------------
        Task CadastrarOuAtualizarAsync(Guid userId, string token);
    }
}
