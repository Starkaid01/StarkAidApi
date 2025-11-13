using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.Devices;
using StarkAid.Api.Entities;
using System.Text.Json;

namespace StarkAid.Api.Services.Devices
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

        public async Task<DisparoResponse> RegistrarDisparoAsync(Guid userId, Guid dispositivoId, string mensagem)
        {
            // Obter nome do dispositivo
            var dispositivoNome = await _context.DispositivosDisparo
                .Where(d => d.Id == dispositivoId)
                .Select(d => d.Nome)
                .FirstOrDefaultAsync() ?? "Desconhecido";

            var disparo = new Disparo
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DispositivoId = dispositivoId,
                DisparadoEm = DateTimeOffset.UtcNow,
                Mensagem = mensagem
            };

            _context.Disparos.Add(disparo);
            await _context.SaveChangesAsync();

            // Publicar no MQTT
            var dispositivo = await _context.DispositivosDisparo.FindAsync(dispositivoId);
            if (dispositivo != null)
            {
                var payload = new { dispositivo = dispositivo.Nome, mensagem, data = disparo.DisparadoEm.ToString("O") };
                await _mqttService.PublishAsync(dispositivo.StatusTopic, JsonSerializer.Serialize(payload));
            }

            // Retornar DTO
            return new DisparoResponse
            {
                Id = disparo.Id,
                DispositivoId = dispositivoId,
                DispositivoNome = dispositivoNome,
                DisparadoEm = disparo.DisparadoEm,
                Mensagem = mensagem,
                Confirmado = false
            };
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
            return await _context.Disparos
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.DisparadoEm)
                .Select(d => new DisparoResponse // Projeção direta para DTO
                {
                    Id = d.Id,
                    DispositivoId = d.DispositivoId,
                    DispositivoNome = d.Dispositivo.Nome, // Garanta que está carregando
                    DisparadoEm = d.DisparadoEm,
                    Mensagem = d.Mensagem,
                    Confirmado = d.Confirmado,
                    ConfirmadoEm = d.ConfirmadoEm
                })
                .ToListAsync();
        }

        public async Task<bool> ConfirmarDisparoAsync(Guid id, Guid userId)
        {
            var disparo = await _context.Disparos
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (disparo == null) return false;

            disparo.Confirmado = true;
            disparo.ConfirmadoEm = DateTimeOffset.UtcNow; // ✅ Corrigido para DateTimeOffset

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