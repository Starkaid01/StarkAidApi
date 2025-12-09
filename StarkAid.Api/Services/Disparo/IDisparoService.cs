using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarkAid.Api.DTOs.Devices;

namespace StarkAid.Api.Services.Disparo;

public interface IDisparoService
{
    Task<DisparoResponse> RegisterAsync(Guid userId, Guid dispositivoId, string mensagem);
    Task<IReadOnlyCollection<DisparoResponse>> ListByUserAsync(Guid userId);
    Task<bool> ConfirmAsync(Guid id, Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);
}
