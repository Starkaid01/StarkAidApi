using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.V1.Comodos
{
    public class EscopoConversacionalService : IEscopoConversacionalService
    {
        private readonly AppDbContext _context;

        public EscopoConversacionalService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EscopoConversacional?> GetEscopoAtivoAsync(Guid userId)
        {
            var agora = DateTimeOffset.UtcNow;
            
            // Cleanup expired first (lazy cleanup, or just filter)
            // But requirement says "Escopo expira automaticamente".
            // We can just query for non-expired.
            
            var escopo = await _context.EscoposConversacionais
                .Include(e => e.Comodo)
                .Where(e => e.UserId == userId && e.ExpiraEm > agora)
                .OrderByDescending(e => e.CriadoEm) // Should be only one, but get latest
                .FirstOrDefaultAsync();

            return escopo;
        }

        public async Task CriarOuRenovarEscopoAsync(Guid userId, Guid comodoId)
        {
            var agora = DateTimeOffset.UtcNow;
            var expiraEm = agora.AddMinutes(10); // TTL 10 min

            // check existing
            var existing = await _context.EscoposConversacionais
                .Where(e => e.UserId == userId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.ComodoId = comodoId;
                existing.ExpiraEm = expiraEm;
                existing.CriadoEm = agora;
                _context.EscoposConversacionais.Update(existing);
            }
            else
            {
                var novo = new EscopoConversacional
                {
                    UserId = userId,
                    ComodoId = comodoId,
                    ExpiraEm = expiraEm,
                    CriadoEm = agora
                };
                await _context.EscoposConversacionais.AddAsync(novo);
            }

            await _context.SaveChangesAsync();
        }

        public async Task LimparEscopoAsync(Guid userId)
        {
            var existing = await _context.EscoposConversacionais
                .Where(e => e.UserId == userId)
                .ExecuteDeleteAsync(); // Efficient delete
        }
    }
}
