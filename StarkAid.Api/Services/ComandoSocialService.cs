using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services;

public class ComandoSocialService
{
    private readonly AppDbContext _context;

    public ComandoSocialService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ComandoSocial>> GetAllAsync()
    {
        return await _context.ComandosSociais.ToListAsync();
    }

    public async Task<ComandoSocial> AddAsync(string comando, string resposta)
    {
        var novo = new ComandoSocial
        {
            Id = Guid.NewGuid(),
            Comando = comando.ToLower(),  // pra evitar case sensitive
            Resposta = resposta
        };

        _context.ComandosSociais.Add(novo);
        await _context.SaveChangesAsync();

        return novo;
    }

    public async Task<List<ComandoSocial>> GetByUserIdAsync(Guid userId)
    {
        return await _context.ComandosSociais
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }

    public async Task<ComandoSocial> AddAsync(Guid userId, string comando, string resposta)
    {
        var novo = new ComandoSocial
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Comando = comando,
            Resposta = resposta
        };

        _context.ComandosSociais.Add(novo);
        await _context.SaveChangesAsync();
        return novo;
    }

    public async Task<bool> EditAsync(Guid id, Guid userId, string comando, string resposta)
    {
        var comandoSocial = await _context.ComandosSociais
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (comandoSocial == null) return false;

        comandoSocial.Comando = comando;
        comandoSocial.Resposta = resposta;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var comandoSocial = await _context.ComandosSociais
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (comandoSocial == null) return false;

        _context.ComandosSociais.Remove(comandoSocial);
        await _context.SaveChangesAsync();
        return true;
    }
}
