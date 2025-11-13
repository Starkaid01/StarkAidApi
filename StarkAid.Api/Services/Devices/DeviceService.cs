using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace StarkAid.Api.Services.Devices
{
    public class DeviceService
    {
        private readonly AppDbContext _context;

        public DeviceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Device>> GetDevicesByUserIdAsync(Guid userId)
        {
            return await _context.Devices
                .Where(d => d.UserId == userId)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByApiKeyAsync(string apiKey)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.ApiKey == apiKey);
        }

        public async Task<Device?> GetByIdAsync(Guid deviceId)
        {
            return await _context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
        }


        public async Task<Device> CreateDeviceAsync(string name, Guid userId, string comando)
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

        public async Task<bool> RenameDeviceAsync(Guid deviceId, Guid userId, string newName, string newComando)
        {
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

            if (device == null)
                return false;

            device.Name = newName;
            device.Comando = newComando; // Atualiza o comando também, se necessário
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<(Device device, bool exists)> PairDeviceAsync(string apiKey, string deviceName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ApiKey == apiKey);
            if (user == null)
                return (null, false);

            // Verifica se já existe o dispositivo com esse nome para esse usuário
            var existingDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.Name == deviceName);

            if (existingDevice != null)
                return (existingDevice, true);

            // Se não existe, cria
            var deviceId = Guid.NewGuid();
            var topic = $"starkaid/{user.Id}/{deviceId}/commands";

            var newDevice = new Device
            {
                Id = deviceId,
                Name = deviceName,
                ApiKey = apiKey,
                UserId = user.Id,
                MqttTopic = topic
            };

            _context.Devices.Add(newDevice);
            await _context.SaveChangesAsync();

            return (newDevice, false);
        }

        public async Task<bool> DeleteDeviceAsync(Guid deviceId, Guid userId)
        {
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

            if (device == null)
                return false;

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();

            return true;
        }

    }
}
