using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarkAid.Api.Services.Devices
{
    public class AgendamentoService : IAgendamentoService
    {
        private readonly AppDbContext _context;
        private readonly DeviceService _deviceService;
        private readonly IMqttClientService _mqttClient;
        private readonly ILogger<AgendamentoService> _logger;

        public AgendamentoService(AppDbContext context, IMqttClientService mqttClient, DeviceService deviceService, ILogger<AgendamentoService> logger)
        {
            _context = context;
            _mqttClient = mqttClient;
            _deviceService = deviceService;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<Agendamento>> ListByUserAsync(Guid userId)
        {
            return await _context.Agendamentos
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.AgendadoPara)
                .ToListAsync();
        }

        // Mantém compatibilidade com código existente
        public async Task<List<Agendamento>> BuscarPorUsuarioAsync(Guid userId)
        {
            var agendamentos = await _context.Agendamentos
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.AgendadoPara)
                .ToListAsync();

            // Garantir que as propriedades de navegação sejam nulas para evitar problemas de serialização
            foreach (var ag in agendamentos)
            {
                ag.Device = null;
                ag.DispositivoEsp = null;
                ag.User = null!;
            }

            return agendamentos;
        }

        public async Task<Agendamento> CreateAsync(Guid userId, Guid deviceId, DateTimeOffset agendadoPara, string comando, string? recorrencia)
        {
            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceId = deviceId,
                TipoAgendamento = TipoAgendamento.Starkswitch,
                AgendadoPara = agendadoPara,
                Comando = comando,
                Recorrencia = recorrencia,
                Executado = false
            };

            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();
            return agendamento;
        }

        // Mantém compatibilidade com código existente
        public async Task<Agendamento> CriarAsync(Guid userId, Guid deviceId, DateTime agendadoPara, string comando, string? recorrencia = null)
        {
            var agendamentoUtc = agendadoPara.Kind == DateTimeKind.Utc
                ? agendadoPara
                : agendadoPara.ToUniversalTime();

            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceId = deviceId,
                TipoAgendamento = TipoAgendamento.Starkswitch,
                AgendadoPara = agendamentoUtc,
                Comando = comando,
                Recorrencia = recorrencia
            };

            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();
            return agendamento;
        }

        // Criar agendamento para dispositivo ESP
        public async Task<Agendamento> CriarAgendamentoEspAsync(Guid userId, Guid dispositivoEspId, DateTime data, int hora, int minuto, string recorrencia)
        {
            // Criar DateTime local e converter para DateTimeOffset UTC
            var dataHoraLocal = new DateTime(data.Year, data.Month, data.Day, hora, minuto, 0, DateTimeKind.Local);
            var timeZone = TimeZoneInfo.Local;
            var agendamentoUtc = TimeZoneInfo.ConvertTimeToUtc(dataHoraLocal, timeZone);
            var agendamentoOffset = new DateTimeOffset(agendamentoUtc, TimeSpan.Zero);

            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DispositivoEspId = dispositivoEspId,
                TipoAgendamento = TipoAgendamento.ESP,
                AgendadoPara = agendamentoOffset,
                Comando = "enviar-comando", // Comando será enviado usando o comando do dispositivo ESP
                Recorrencia = recorrencia,
                Executado = false
            };

            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();
            return agendamento;
        }

        // Criar agendamento para dispositivo Starkswitch
        public async Task<Agendamento> CriarAgendamentoStarkswitchAsync(Guid userId, Guid deviceId, string acao, DateTime data, int hora, int minuto, string recorrencia)
        {
            // Criar DateTime local e converter para DateTimeOffset UTC
            var dataHoraLocal = new DateTime(data.Year, data.Month, data.Day, hora, minuto, 0, DateTimeKind.Local);
            var timeZone = TimeZoneInfo.Local;
            var agendamentoUtc = TimeZoneInfo.ConvertTimeToUtc(dataHoraLocal, timeZone);
            var agendamentoOffset = new DateTimeOffset(agendamentoUtc, TimeSpan.Zero);

            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceId = deviceId,
                TipoAgendamento = TipoAgendamento.Starkswitch,
                AgendadoPara = agendamentoOffset,
                Comando = acao.ToLower(), // "ligar" ou "desligar"
                Recorrencia = recorrencia,
                Executado = false
            };

            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();
            return agendamento;
        }

        // Criar agendamento para dispositivo Ewelink
        public async Task<Agendamento> CriarAgendamentoEwelinkAsync(Guid userId, string ewelinkDeviceId, string acao, DateTime data, int hora, int minuto, string recorrencia)
        {
            // Criar DateTime local e converter para DateTimeOffset UTC
            var dataHoraLocal = new DateTime(data.Year, data.Month, data.Day, hora, minuto, 0, DateTimeKind.Local);
            var timeZone = TimeZoneInfo.Local;
            var agendamentoUtc = TimeZoneInfo.ConvertTimeToUtc(dataHoraLocal, timeZone);
            var agendamentoOffset = new DateTimeOffset(agendamentoUtc, TimeSpan.Zero);

            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EwelinkDeviceId = ewelinkDeviceId,
                TipoAgendamento = TipoAgendamento.Ewelink,
                AgendadoPara = agendamentoOffset,
                Comando = acao.ToLower(), // "ligar" ou "desligar"
                Recorrencia = recorrencia,
                Executado = false
            };

            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();
            return agendamento;
        }

        public async Task<bool> UpdateAsync(Guid id, Guid userId, DateTimeOffset agendadoPara, string comando, string? recorrencia)
        {
            var agendamento = await _context.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (agendamento == null) return false;

            agendamento.AgendadoPara = agendadoPara;
            agendamento.Comando = comando;
            agendamento.Recorrencia = recorrencia;

            // Atualiza Executado baseado na nova data/hora
            agendamento.Executado = agendadoPara <= DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Mantém compatibilidade com código existente
        public async Task<bool> EditarAsync(Guid id, Guid userId, DateTime agendadoPara, string comando, string? recorrencia)
        {
            var agendamento = await _context.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (agendamento == null) return false;

            var novoAgendadoUtc = agendadoPara.Kind == DateTimeKind.Utc
                ? agendadoPara
                : agendadoPara.ToUniversalTime();

            agendamento.AgendadoPara = novoAgendadoUtc;
            agendamento.Comando = comando;
            agendamento.Recorrencia = recorrencia;

            // Atualiza Executado baseado na nova data/hora
            agendamento.Executado = novoAgendadoUtc <= DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var agendamento = await _context.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (agendamento == null) return false;

            _context.Agendamentos.Remove(agendamento);
            await _context.SaveChangesAsync();
            return true;
        }

        // Mantém compatibilidade com código existente
        public async Task<bool> ExcluirAsync(Guid id, Guid userId)
        {
            var agendamento = await _context.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (agendamento == null) return false;

            _context.Agendamentos.Remove(agendamento);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyCollection<Agendamento>> GetPendingAsync()
        {
            var agora = DateTimeOffset.UtcNow;
            // Não usar AsNoTracking() para que as entidades sejam rastreadas e possam ser atualizadas
            var pendentes = await _context.Agendamentos
                .Where(a => !a.Executado && a.AgendadoPara <= agora)
                .ToListAsync();
            
            _logger.LogInformation("GetPendingAsync: Agora (UTC) = {Agora}, Encontrados {Count} agendamentos pendentes", 
                agora, pendentes.Count);
            
            return pendentes;
        }

        public async Task UpdateManyAsync(IEnumerable<Agendamento> agendamentos)
        {
            // Como GetPendingAsync não usa AsNoTracking(), as entidades já estão sendo rastreadas
            // Apenas precisamos salvar as mudanças
            var agendamentosList = agendamentos.ToList();
            
            foreach (var ag in agendamentosList)
            {
                _logger.LogInformation("Atualizando agendamento {Id}: Executado={Executado}, AgendadoPara={AgendadoPara}, Recorrencia={Recorrencia}", 
                    ag.Id, ag.Executado, ag.AgendadoPara, ag.Recorrencia);
                
                // Verificar se a entidade está sendo rastreada
                var entry = _context.Entry(ag);
                if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
                {
                    // Se não estiver rastreada, buscar do banco e atualizar
                    var agendamentoNoBanco = await _context.Agendamentos.FindAsync(ag.Id);
                    if (agendamentoNoBanco != null)
                    {
                        _logger.LogInformation("Agendamento {Id} estava detached, buscando do banco e atualizando", ag.Id);
                        agendamentoNoBanco.Executado = ag.Executado;
                        agendamentoNoBanco.AgendadoPara = ag.AgendadoPara;
                        agendamentoNoBanco.Recorrencia = ag.Recorrencia;
                    }
                }
                else
                {
                    // Se já estiver rastreada, apenas marcar como modificada
                    entry.Property(a => a.Executado).IsModified = true;
                    entry.Property(a => a.AgendadoPara).IsModified = true;
                    entry.Property(a => a.Recorrencia).IsModified = true;
                }
            }
            
            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation("Salvadas {Count} alterações de agendamentos", saved);
        }

        // Mantém compatibilidade com código existente
        public async Task<List<Agendamento>> BuscarAgendamentosPendentesAsync()
        {
            var agora = DateTime.UtcNow;
            return await _context.Agendamentos
                .Where(a => !a.Executado && a.AgendadoPara <= agora)
                .ToListAsync();
        }

        public async Task MarcarComoExecutadoAsync(Guid agendamentoId)
        {
            var agendamento = await _context.Agendamentos.FindAsync(agendamentoId);
            if (agendamento != null)
            {
                agendamento.Executado = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AtualizarAgendamentoAsync(Agendamento agendamento)
        {
            _context.Agendamentos.Update(agendamento);
            await _context.SaveChangesAsync();
        }
    }
}
