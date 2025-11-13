using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.Devices
{
    public class AgendamentoService
    {
        private readonly AppDbContext _context;
        private readonly DeviceService _deviceService;

        private readonly IMqttClientService _mqttClient;

        public AgendamentoService(AppDbContext context, IMqttClientService mqttClient, DeviceService deviceService)
        {
            _context = context;
            _mqttClient = mqttClient;
            _deviceService = deviceService;
        }

        public async Task<List<Agendamento>> BuscarPorUsuarioAsync(Guid userId)
        {
            return await _context.Agendamentos
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.AgendadoPara)
                .ToListAsync();
        }

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
                AgendadoPara = agendamentoUtc,
                Comando = comando,
                Recorrencia = recorrencia
            };

            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();
            return agendamento;
        }

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

        
        public async Task AtualizarAgendamentoAsync(Agendamento agendamento)
        {
            _context.Agendamentos.Update(agendamento);
            await _context.SaveChangesAsync();
        }
        public async Task<bool> ExcluirAsync(Guid id, Guid userId)
        {
            var agendamento = await _context.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (agendamento == null) return false;

            _context.Agendamentos.Remove(agendamento);
            await _context.SaveChangesAsync();
            return true;
        }

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
    }

}
