using StarkAid.WindowsForms.Database;
using StarkAid.WindowsForms.Models;
using StarkAid.WindowsForms.Services;

namespace StarkAid.WindowsForms.Services;

public class LicenseService
{
    private readonly ApiService _apiService;
    private readonly LocalDatabase _database;
    private const string LicenseKeyKey = "LicenseKey";
    private const string MachineIdKey = "MachineId";

    public LicenseService(ApiService apiService, LocalDatabase database)
    {
        _apiService = apiService;
        _database = database;
    }

    public string? GetStoredLicenseKey()
    {
        return _database.GetSetting(LicenseKeyKey);
    }

    public void SaveLicenseKey(string licenseKey)
    {
        _database.SaveSetting(LicenseKeyKey, licenseKey);
    }

    public string GetMachineId()
    {
        var stored = _database.GetSetting(MachineIdKey);
        if (!string.IsNullOrEmpty(stored))
        {
            return stored;
        }

        // Gerar um identificador único da máquina
        try
        {
            var machineName = Environment.MachineName;
            var userName = Environment.UserName;
            var processorId = Environment.ProcessorCount.ToString();
            
            var combined = $"{machineName}-{userName}-{processorId}";
            
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
            var machineId = Convert.ToBase64String(hash);
            
            _database.SaveSetting(MachineIdKey, machineId);
            return machineId;
        }
        catch
        {
            var machineId = Guid.NewGuid().ToString();
            _database.SaveSetting(MachineIdKey, machineId);
            return machineId;
        }
    }

    public async Task<bool> VerifyLicenseAsync()
    {
        var licenseKey = GetStoredLicenseKey();
        if (string.IsNullOrEmpty(licenseKey))
        {
            return false;
        }

        try
        {
            // Verificar se há internet
            if (!IsInternetAvailable())
            {
                // Se não há internet, assumir que a licença está válida (modo offline)
                // Mas isso pode ser melhorado com cache de verificação
                return true;
            }

            return await _apiService.VerifyLicenseAsync(licenseKey);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar licença: {ex.Message}");
            // Em caso de erro, assumir válida para não bloquear o usuário
            return true;
        }
    }

    public async Task<bool> ActivateLicenseAsync(string licenseKey, string? machineName = null)
    {
        try
        {
            if (!IsInternetAvailable())
            {
                throw new Exception("É necessário conexão com a internet para ativar a licença.");
            }

            // Normalizar a chave da licença
            var normalizedKey = licenseKey?.Trim().ToUpperInvariant() ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"[LicenseService] Tentando ativar licença: {normalizedKey}");
            System.Diagnostics.Debug.WriteLine($"[LicenseService] Token presente: {!string.IsNullOrEmpty(_apiService.GetToken())}");
            System.Diagnostics.Debug.WriteLine($"[LicenseService] MachineName: {machineName ?? Environment.MachineName}");

            var activation = await _apiService.ActivateLicenseAsync(normalizedKey, machineName ?? Environment.MachineName);
            
            if (activation != null)
            {
                System.Diagnostics.Debug.WriteLine($"[LicenseService] Licença ativada com sucesso!");
                SaveLicenseKey(normalizedKey);
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"[LicenseService] Falha na ativação - activation é null");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LicenseService] Erro ao ativar licença: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LicenseService] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private bool IsInternetAvailable()
    {
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = ping.Send("8.8.8.8", 3000);
            return reply?.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}

