using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.Devices
{
    public class DispositivoDisparoService
    {
        private readonly AppDbContext _context;

        public DispositivoDisparoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DispositivoDisparo> CriarAsync(Guid userId, string nome)
        {
            var dispositivo = new DispositivoDisparo
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Nome = nome,
                MqttTopic = $"starkaid/{userId}/{Guid.NewGuid()}",
                StatusTopic = $"starkaid/{userId}/{Guid.NewGuid()}/status"
            };

            _context.DispositivosDisparo.Add(dispositivo);
            await _context.SaveChangesAsync();
            return dispositivo;
        }

        public async Task<bool> EditarAsync(Guid id, Guid userId, string nome)
        {
            var dispositivo = await _context.DispositivosDisparo
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (dispositivo == null) return false;

            dispositivo.Nome = nome;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DispositivoDisparo>> ListarPorUsuarioAsync(Guid userId)
        {
            return await _context.DispositivosDisparo
                .Where(d => d.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> ExcluirAsync(Guid id, Guid userId)
        {
            var dispositivo = await _context.DispositivosDisparo
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (dispositivo == null) return false;

            _context.DispositivosDisparo.Remove(dispositivo);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<DispositivoDisparo?> ObterPorIdAsync(Guid id, Guid userId)
        {
            return await _context.DispositivosDisparo
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        }
    }
}
