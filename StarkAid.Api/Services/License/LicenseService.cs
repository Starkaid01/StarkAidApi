using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarkAid.Api.Data;
using StarkAid.Api.Entities;
using System.Security.Cryptography;
using System.Text;
using LicenseEntity = StarkAid.Api.Entities.License;

namespace StarkAid.Api.Services.License;

public class LicenseService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LicenseService> _logger;
    private readonly StarkAid.Api.Services.Notifications.NotificationService? _notificationService;

    public LicenseService(
        AppDbContext context, 
        ILogger<LicenseService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        
        // Obter NotificationService via service provider (pode ser null se não estiver registrado)
        try
        {
            _notificationService = serviceProvider.GetService<StarkAid.Api.Services.Notifications.NotificationService>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NotificationService não disponível");
            _notificationService = null;
        }
    }

    public async Task<LicenseEntity> CreateLicenseAsync(Guid userId, int maxMachines, decimal price, string? stripeSessionId = null)
    {
        var licenseKey = GenerateLicenseKey();
        
        var license = new LicenseEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LicenseKey = licenseKey,
            MaxMachines = maxMachines,
            Price = price,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddYears(100), // Licença vitalícia (100 anos)
            IsActive = false, // Será ativada após confirmação do pagamento
            StripeSessionId = stripeSessionId
        };

        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Licença criada: {LicenseId} para usuário {UserId}", license.Id, userId);

        return license;
    }

    public async Task<LicenseEntity?> GetLicenseByKeyAsync(string licenseKey)
    {
        // Normalizar a chave: remover espaços e converter para maiúsculas
        var normalizedKey = licenseKey?.Trim().ToUpperInvariant() ?? string.Empty;
        
        // Buscar todas as licenças e comparar em memória (já que ToUpperInvariant não é traduzido pelo EF)
        var licenses = await _context.Licenses
            .Include(l => l.User)
            .Include(l => l.Activations)
            .ToListAsync();
        
        // Comparar em memória usando ToUpperInvariant
        return licenses.FirstOrDefault(l => 
            l.LicenseKey.Trim().ToUpperInvariant() == normalizedKey);
    }

    public async Task<LicenseEntity?> GetLicenseByIdAsync(Guid licenseId)
    {
        return await _context.Licenses
            .Include(l => l.User)
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Id == licenseId);
    }

    public async Task<List<LicenseEntity>> GetUserLicensesAsync(Guid userId)
    {
        return await _context.Licenses
            .Include(l => l.Activations)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<LicenseActivation?> ActivateLicenseAsync(string licenseKey, string machineId, string? machineName = null, string? ipAddress = null)
    {
        var license = await GetLicenseByKeyAsync(licenseKey);
        
        if (license == null)
        {
            _logger.LogWarning("Tentativa de ativar licença inexistente: {LicenseKey}", licenseKey);
            return null;
        }

        if (!license.IsActive)
        {
            _logger.LogWarning("Tentativa de ativar licença inativa: {LicenseKey}", licenseKey);
            return null;
        }

        // Verificar se a máquina já está ativada nesta licença
        var existingActivation = await _context.LicenseActivations
            .FirstOrDefaultAsync(la => la.LicenseId == license.Id && la.MachineId == machineId && la.IsActive);

        if (existingActivation != null)
        {
            _logger.LogInformation("Máquina já está ativada nesta licença: {MachineId}", machineId);
            return existingActivation;
        }

        // Contar ativações ativas
        var activeActivations = await _context.LicenseActivations
            .CountAsync(la => la.LicenseId == license.Id && la.IsActive);

        if (activeActivations >= license.MaxMachines)
        {
            _logger.LogWarning("Limite de máquinas atingido para licença {LicenseKey}. Máquinas ativas: {Count}, Máximo: {Max}", 
                licenseKey, activeActivations, license.MaxMachines);
            return null;
        }

        // Criar nova ativação
        var activation = new LicenseActivation
        {
            Id = Guid.NewGuid(),
            LicenseId = license.Id,
            MachineId = machineId,
            MachineName = machineName,
            ActivatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            IpAddress = ipAddress
        };

        _context.LicenseActivations.Add(activation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Licença ativada para máquina {MachineId} na licença {LicenseKey}", machineId, licenseKey);

        return activation;
    }

    public async Task<bool> DeactivateLicenseAsync(string licenseKey, string machineId)
    {
        var license = await GetLicenseByKeyAsync(licenseKey);
        
        if (license == null)
        {
            return false;
        }

        var activation = await _context.LicenseActivations
            .FirstOrDefaultAsync(la => la.LicenseId == license.Id && la.MachineId == machineId && la.IsActive);

        if (activation == null)
        {
            return false;
        }

        activation.IsActive = false;
        activation.DeactivatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Licença desativada para máquina {MachineId} na licença {LicenseKey}", machineId, licenseKey);

        return true;
    }

    public async Task<bool> VerifyLicenseAsync(string licenseKey, string machineId)
    {
        var license = await GetLicenseByKeyAsync(licenseKey);
        
        if (license == null || !license.IsActive)
        {
            return false;
        }

        // Verificar se a máquina está ativada
        var activation = await _context.LicenseActivations
            .FirstOrDefaultAsync(la => la.LicenseId == license.Id && la.MachineId == machineId && la.IsActive);

        return activation != null;
    }

    public async Task<bool> ConfirmPaymentAsync(string stripeSessionId, string? paymentIntentId = null)
    {
        var license = await _context.Licenses
            .FirstOrDefaultAsync(l => l.StripeSessionId == stripeSessionId);

        if (license == null)
        {
            _logger.LogWarning("Licença não encontrada para sessão Stripe: {SessionId}", stripeSessionId);
            return false;
        }

        license.IsActive = true;
        license.PaymentConfirmedAt = DateTimeOffset.UtcNow;
        license.StripePaymentIntentId = paymentIntentId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pagamento confirmado para licença {LicenseId}", license.Id);

        // Criar notificação para administrador
        if (_notificationService != null)
        {
            try
            {
                var user = await _context.Users.FindAsync(license.UserId);
                if (user != null)
                {
                    await _notificationService.CriarNotificacaoAsync(
                        "licenca",
                        $"Nova Licença de Software - {license.MaxMachines} máquina(s)",
                        $"Usuário {user.Name} ({user.Email}) comprou uma licença para {license.MaxMachines} máquina(s) por R$ {license.Price:F2}.",
                        user.Id,
                        user.Email,
                        user.Name,
                        license.Price,
                        license.Id.ToString()
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar notificação de licença");
            }
        }

        return true;
    }

    public async Task<List<LicenseActivation>> GetLicenseActivationsAsync(Guid licenseId)
    {
        return await _context.LicenseActivations
            .Where(la => la.LicenseId == licenseId)
            .OrderByDescending(la => la.ActivatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteInactiveLicenseAsync(Guid licenseId, Guid userId)
    {
        var license = await GetLicenseByIdAsync(licenseId);
        
        if (license == null)
        {
            _logger.LogWarning("Licença não encontrada: {LicenseId}", licenseId);
            return false;
        }

        // Verificar se a licença pertence ao usuário
        if (license.UserId != userId)
        {
            _logger.LogWarning("Usuário {UserId} tentou deletar licença de outro usuário: {LicenseId}", userId, licenseId);
            return false;
        }

        // Só permitir deletar licenças inativas
        if (license.IsActive)
        {
            _logger.LogWarning("Tentativa de deletar licença ativa: {LicenseId}", licenseId);
            return false;
        }

        // Deletar todas as ativações primeiro (cascade)
        var activations = await _context.LicenseActivations
            .Where(la => la.LicenseId == licenseId)
            .ToListAsync();
        
        _context.LicenseActivations.RemoveRange(activations);
        
        // Deletar a licença
        _context.Licenses.Remove(license);
        
        await _context.SaveChangesAsync();

        _logger.LogInformation("Licença inativa deletada: {LicenseId} pelo usuário {UserId}", licenseId, userId);

        return true;
    }

    private string GenerateLicenseKey()
    {
        // Gerar uma chave de licença única no formato: STARK-XXXX-XXXX-XXXX-XXXX
        var random = new Random();
        var segments = new List<string> { "STARK" };
        
        for (int i = 0; i < 4; i++)
        {
            var segment = new StringBuilder();
            for (int j = 0; j < 4; j++)
            {
                segment.Append((char)('A' + random.Next(26)));
            }
            segments.Add(segment.ToString());
        }

        var licenseKey = string.Join("-", segments);

        // Verificar se já existe
        if (_context.Licenses.Any(l => l.LicenseKey == licenseKey))
        {
            // Se existir, gerar novamente (recursão)
            return GenerateLicenseKey();
        }

        return licenseKey;
    }
}

