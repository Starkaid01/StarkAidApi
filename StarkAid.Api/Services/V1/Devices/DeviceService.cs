using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace StarkAid.Api.Services.V1.Devices;

public class DeviceService : IDeviceService
{
    private readonly AppDbContext _context;

    public DeviceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Device>> GetByUserAsync(Guid userId)
    {
        return await _context.Devices
            .Where(d => d.UserId == userId)
            .ToListAsync();
    }

    public async Task<Device?> GetByIdAsync(Guid deviceId)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId);
    }

    public async Task<Device> CreateAsync(string name, Guid userId, string comando)
    {
        var deviceId = Guid.NewGuid();
        var topic = $"starkaid/{userId}/{deviceId}/commands";

        var device = new Device
        {
            Id = deviceId,
            Name = name,
            UserId = userId,
            ApiKey = Guid.NewGuid().ToString(),
            MqttTopic = topic,
            Comando = comando
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();
        return device;
    }

    public async Task<bool> RenameAsync(Guid deviceId, Guid userId, string newName, string? novoComando)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

        if (device == null)
            return false;

        device.Name = newName;
        device.Comando = novoComando;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid deviceId, Guid userId)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

        if (device == null)
            return false;

        _context.Devices.Remove(device);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(Device device, bool existed)> PairAsync(string apiKey, string deviceName)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ApiKey == apiKey);

        if (user == null)
            return (null, false);

        var existing = await _context.Devices
            .FirstOrDefaultAsync(d => d.UserId == user.Id && d.Name == deviceName);

        if (existing != null)
            return (existing, true);

        var id = Guid.NewGuid();
        var topic = $"starkaid/{user.Id}/{id}/commands";

        var device = new Device
        {
            Id = id,
            Name = deviceName,
            ApiKey = apiKey,
            UserId = user.Id,
            MqttTopic = topic
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        return (device, false);
    }
}
