using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Dtos;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services
{
    public class DisparoService
    {
        private readonly AppDbContext _context;

        private readonly IMqttClientService _mqttService;

        public DisparoService(AppDbContext context, IMqttClientService mqttService)
        {
            _context = context;
            _mqttService = mqttService;
        }

        public async Task<Disparo> RegistrarDisparoAsync(Guid userId, Guid dispositivoId, string mensagem)
        {
            var disparo = new Disparo
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DispositivoId = dispositivoId,
                DisparadoEm = DateTime.UtcNow,
                Mensagem = mensagem
            };

            _context.Disparos.Add(disparo);
            await _context.SaveChangesAsync();

            // Obtém o dispositivo para pegar o tópico MQTT
            var dispositivo = await _context.DispositivosDisparo
                .FirstOrDefaultAsync(d => d.Id == dispositivoId && d.UserId == userId);

            if (dispositivo != null)
            {
                var payload = new
                {
                    dispositivo = dispositivo.Nome,
                    mensagem,
                    data = disparo.DisparadoEm.ToString("O") // ISO 8601
                };

                var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

                await _mqttService.PublishAsync(dispositivo.StatusTopic, payloadJson);
            }

            return disparo;
        }

        public async Task<List<Disparo>> ListarPorUsuarioAsync(Guid userId)
        {
            return await _context.Disparos
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.DisparadoEm)
                .ToListAsync();
        }

        public async Task<List<DisparoResponse>> ListarDisparosComNomePorUsuarioAsync(Guid userId)
        {
            var disparos = await _context.Disparos
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.DisparadoEm)
                .Join(
                    _context.DispositivosDisparo,
                    disparo => disparo.DispositivoId,
                    dispositivo => dispositivo.Id,
                    (disparo, dispositivo) => new DisparoResponse
                    {
                        Id = disparo.Id,
                        DispositivoId = disparo.DispositivoId,
                        DispositivoNome = dispositivo.Nome,
                        DisparadoEm = disparo.DisparadoEm,
                        Mensagem = disparo.Mensagem,
                        Confirmado = disparo.Confirmado,
                        ConfirmadoEm = disparo.ConfirmadoEm
                    }
                )
                .ToListAsync();

            return disparos;
        }

        public async Task<bool> ConfirmarDisparoAsync(Guid id, Guid userId)
        {
            var disparo = await _context.Disparos
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (disparo == null) return false;

            disparo.Confirmado = true;
            disparo.ConfirmadoEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExcluirAsync(Guid id, Guid userId)
        {
            var disparo = await _context.Disparos
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (disparo == null) return false;

            _context.Disparos.Remove(disparo);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
