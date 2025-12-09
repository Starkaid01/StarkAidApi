namespace StarkAid.WindowsForms.Models;

public class License
{
    public Guid Id { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public int MaxMachines { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? PaymentConfirmedAt { get; set; }
    public int ActiveActivations { get; set; }
    public List<LicenseActivation> Activations { get; set; } = new();
}

public class LicenseActivation
{
    public Guid Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public DateTimeOffset ActivatedAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? IpAddress { get; set; }
}

public class ActivateLicenseRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string? MachineName { get; set; }
}

public class VerifyLicenseRequest
{
    public string LicenseKey { get; set; } = string.Empty;
}

public class VerifyLicenseResponse
{
    public bool IsValid { get; set; }
}

