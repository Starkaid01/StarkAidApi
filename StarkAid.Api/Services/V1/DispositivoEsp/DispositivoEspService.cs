using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DispositivoEspEntity = StarkAid.Api.Entities.DispositivoEsp;

namespace StarkAid.Api.Services.V1.DispositivoEsp;

public class DispositivoEspService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DispositivoEspService> _logger;

    public DispositivoEspService(AppDbContext context, ILogger<DispositivoEspService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<DispositivoEspEntity>> GetAllAsync(Guid? userId = null)
    {
        var query = _context.DispositivosEsp.AsQueryable();
        
        if (userId.HasValue)
        {
            query = query.Where(d => d.UserId == userId || d.UserId == null);
        }

        return await query.OrderBy(d => d.Nome).ToListAsync();
    }

    public async Task<DispositivoEspEntity?> GetByIdAsync(Guid id)
    {
        return await _context.DispositivosEsp.FindAsync(id);
    }

    public async Task<DispositivoEspEntity> CreateAsync(string nome, string ip, int porta, string? comando, string? comandToEsp, Guid? userId = null)
    {
        var dispositivo = new DispositivoEspEntity
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Ip = ip,
            Porta = porta,
            Comando = comando,
            ComandToEsp = comandToEsp,
            Status = "Desconectado",
            LigadoDesligado = false,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.DispositivosEsp.Add(dispositivo);
        await _context.SaveChangesAsync();

        return dispositivo;
    }

    public async Task<bool> UpdateAsync(Guid id, string? nome, string? ip, int? porta, string? comando, string? comandToEsp, string? status, bool? ligadoDesligado)
    {
        var dispositivo = await _context.DispositivosEsp.FindAsync(id);
        if (dispositivo == null) return false;

        if (!string.IsNullOrWhiteSpace(nome))
            dispositivo.Nome = nome;

        if (!string.IsNullOrWhiteSpace(ip))
            dispositivo.Ip = ip;

        if (porta.HasValue)
            dispositivo.Porta = porta.Value;

        if (comando != null)
            dispositivo.Comando = comando;

        if (comandToEsp != null)
            dispositivo.ComandToEsp = comandToEsp;

        if (!string.IsNullOrWhiteSpace(status))
            dispositivo.Status = status;

        if (ligadoDesligado.HasValue)
            dispositivo.LigadoDesligado = ligadoDesligado.Value;

        dispositivo.LastUpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var dispositivo = await _context.DispositivosEsp.FindAsync(id);
        if (dispositivo == null) return false;

        _context.DispositivosEsp.Remove(dispositivo);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PingAsync(Guid id)
    {
        var dispositivo = await _context.DispositivosEsp.FindAsync(id);
        if (dispositivo == null) return false;

        try
        {
            // Tenta fazer ping no IP
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(dispositivo.Ip, 3000);

            if (reply.Status == IPStatus.Success)
            {
                dispositivo.Status = "Conectado";
                dispositivo.LastPingAt = DateTimeOffset.UtcNow;
            }
            else
            {
                dispositivo.Status = "Desconectado";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao fazer ping no dispositivo {Id} - {Ip}", id, dispositivo.Ip);
            dispositivo.Status = "Desconectado";
        }

        await _context.SaveChangesAsync();
        return dispositivo.Status == "Conectado";
    }

    public async Task<bool> PingByIpAsync(string ip)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip, 3000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task UpdateStatusAsync(Guid id, string status, bool? ligadoDesligado = null)
    {
        var dispositivo = await _context.DispositivosEsp.FindAsync(id);
        if (dispositivo == null) return;

        dispositivo.Status = status;
        dispositivo.LastPingAt = DateTimeOffset.UtcNow;

        if (ligadoDesligado.HasValue)
            dispositivo.LigadoDesligado = ligadoDesligado.Value;

        await _context.SaveChangesAsync();
    }

    public async Task<DispositivoEspEntity?> FindByComandoAsync(string comando)
    {
        return await _context.DispositivosEsp
            .FirstOrDefaultAsync(d => d.Comando != null && d.Comando.ToLower().Contains(comando.ToLower()));
    }

    public async Task<List<DispositivoEspEntity>> FindByComandoPartialAsync(string comando)
    {
        var comandoLower = comando.ToLower();
        return await _context.DispositivosEsp
            .Where(d => d.Comando != null && d.Comando.ToLower().Contains(comandoLower))
            .ToListAsync();
    }
}

