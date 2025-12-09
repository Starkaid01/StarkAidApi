using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.Devices;

public interface IAgendamentoService
{
    Task<IReadOnlyCollection<Agendamento>> ListByUserAsync(Guid userId);
    Task<Agendamento> CreateAsync(Guid userId, Guid deviceId, DateTimeOffset agendadoPara, string comando, string? recorrencia);
    Task<bool> UpdateAsync(Guid id, Guid userId, DateTimeOffset agendadoPara, string comando, string? recorrencia);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<IReadOnlyCollection<Agendamento>> GetPendingAsync();

    // Novo método: grava uma lista de agendamentos já modificados.
    Task UpdateManyAsync(IEnumerable<Agendamento> agendamentos);
}
