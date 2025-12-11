using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services.V1.Firebase
{
    public class FirebaseTokenService : IFirebaseTokenService
    {
        private readonly AppDbContext _db;

        public FirebaseTokenService(AppDbContext db) => _db = db;

        public async Task SaveOrUpdateAsync(Guid userId, string token)
        {
            var antigos = _db.FirebaseTokens.Where(t => t.UserId == userId);
            _db.FirebaseTokens.RemoveRange(antigos);

            var novo = new FirebaseToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                DataCadastro = DateTimeOffset.UtcNow
            };

            _db.FirebaseTokens.Add(novo);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyCollection<string>> GetTokensAsync(Guid userId) =>
            await _db.FirebaseTokens
                     .Where(t => t.UserId == userId)
                     .Select(t => t.Token)
                     .ToListAsync();

        public async Task DeleteAsync(Guid userId, string token)
        {
            var entity = await _db.FirebaseTokens
                                   .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token);

            if (entity != null)
            {
                _db.FirebaseTokens.Remove(entity);
                await _db.SaveChangesAsync();
            }
        }

        // -------------------------------------------------
        // Wrapper – simplesmente delega para o método já existente
        // -------------------------------------------------
        public Task CadastrarOuAtualizarAsync(Guid userId, string token) =>
            SaveOrUpdateAsync(userId, token);
    }
}
