using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarkAid.Api.DTOs.Rotinas;

namespace StarkAid.Api.Services.V1.Rotinas
{
    public interface IRotinaService
    {
        Task<List<RotinaDto>> GetAllAsync(Guid userId);
        Task<RotinaDto?> GetByIdAsync(Guid id, Guid userId);
        Task<RotinaDto> CreateAsync(Guid userId, CreateRotinaRequest request);
        Task<RotinaDto?> UpdateAsync(Guid id, Guid userId, UpdateRotinaRequest request);
        Task<bool> DeleteAsync(Guid id, Guid userId);
        
        Task<bool> SetAtivaAsync(Guid id, Guid userId, bool ativa);
        
        // Execução
        Task ExecutarRotinaAsync(Guid id, Guid userId, int depth = 0);
        Task ProcessarGatilhosTempoAsync(DateTimeOffset agora);
        Task<bool> ProcessarGatilhosComandoAsync(Guid userId, string comando, int depth = 0);
        Task SeedDefaultRotinasAsync(Guid userId);
    }
}
