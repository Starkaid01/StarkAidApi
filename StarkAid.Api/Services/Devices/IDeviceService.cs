using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.Devices;

public interface IDeviceService
{
    Task<IReadOnlyCollection<Device>> GetByUserAsync(Guid userId);
    Task<Device?> GetByIdAsync(Guid deviceId);
    Task<Device> CreateAsync(string name, Guid userId, string comando);
    Task<bool> RenameAsync(Guid deviceId, Guid userId, string newName, string? novoComando);
    Task<bool> DeleteAsync(Guid deviceId, Guid userId);
    Task<(Device device, bool existed)> PairAsync(string apiKey, string deviceName);
}
