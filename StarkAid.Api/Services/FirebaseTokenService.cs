using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services
{
    public class FirebaseTokenService
    {
        private readonly AppDbContext _context;

        public FirebaseTokenService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CadastrarOuAtualizarAsync(Guid userId, string token)
        {
            var existente = await _context.FirebaseTokens
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Token == token);

            if (existente == null)
            {
                var novo = new FirebaseToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = token,
                    DataCadastro = DateTime.UtcNow
                };

                _context.FirebaseTokens.Add(novo);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<string>> ObterTokensPorUsuario(Guid userId)
        {
            return await _context.FirebaseTokens
                .Where(x => x.UserId == userId)
                .Select(x => x.Token)
                .ToListAsync();
        }
    }
}
