using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using System.Text.Json;
using System.Collections.Generic;

namespace StarkAid.Api.Services.V1.Fun
{
    public interface IJokeService
    {
        Task<string> GetRandomJokeAsync(Guid userId);
    }

    public class JokeService : IJokeService
    {
        private readonly AppDbContext _context;

        public JokeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetRandomJokeAsync(Guid userId)
        {
            // 1. Get or Create User State
            var userState = await _context.UserFunStates
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (userState == null)
            {
                userState = new UserFunState { Id = Guid.NewGuid(), UserId = userId };
                _context.UserFunStates.Add(userState);
            }

            // 2. Parse History
            List<int> history = new();
            try
            {
                if (!string.IsNullOrWhiteSpace(userState.PiadasContadasIds))
                    history = JsonSerializer.Deserialize<List<int>>(userState.PiadasContadasIds) ?? new();
            }
            catch { /* Ignore corrupt state */ }

            // 3. Reset if limit reached (10)
            if (history.Count >= 10)
            {
                history.Clear();
            }

            // 4. Find valid jokes excluding history
            var jokesQuery = _context.Piadas.AsQueryable();
            if (history.Any())
            {
                jokesQuery = jokesQuery.Where(p => !history.Contains(p.Id));
            }

            var availableCount = await jokesQuery.CountAsync();

            // If no jokes available (e.g. less than 10 total in DB?), clear history and try again
            if (availableCount == 0 && history.Any())
            {
                history.Clear();
                jokesQuery = _context.Piadas.AsQueryable(); // Reset query
                availableCount = await jokesQuery.CountAsync();
            }

            if (availableCount == 0)
                return "Não conheço nenhuma piada no momento.";

            // 5. Pick random
            // Optimized random pick for SQL: Skip random number
            var rnd = new Random();
            var skip = rnd.Next(0, availableCount);
            
            var joke = await jokesQuery.Skip(skip).FirstOrDefaultAsync();

            if (joke != null)
            {
                // 6. Update State
                history.Add(joke.Id);
                userState.PiadasContadasIds = JsonSerializer.Serialize(history);
                await _context.SaveChangesAsync();

                return joke.Texto;
            }

            return "Não consegui lembrar de uma piada agora.";
        }
    }
}
