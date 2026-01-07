using StarkAid.Api.DTOs.V1.Comodos;

namespace StarkAid.Api.Services.V1.Comodos
{
    public interface IComodoService
    {
        Task<List<ComodoDto>> GetAllAsync(Guid userId);
        Task<ComodoDto?> GetByIdAsync(Guid id, Guid userId);
        Task<ComodoDto> CreateAsync(Guid userId, CreateComodoRequest request);
        Task<ComodoDto?> UpdateAsync(Guid id, Guid userId, UpdateComodoRequest request);
        Task<bool> DeleteAsync(Guid id, Guid userId);
        Task<bool> AddDeviceAsync(Guid comodoId, Guid userId, AssociateDeviceRequest request);
        Task<bool> RemoveDeviceAsync(Guid comodoId, string dispositivoId, Guid userId);
        Task<List<DeviceSelectionDto>> GetAvailableDevicesAsync(Guid userId);
        Task<bool> ToggleDeviceAsync(Guid userId, string dispositivoId, string tipo);
        
        Task<ComandoAmbienteResult> ResolverComandoAmbienteAsync(Guid userId, string tipoDispositivo, string? originalCommand, string? comodoNomeConfirmado = null);
        Task<string> ControlAllDevicesAsync(Guid userId, bool turnOn);
    }
}
