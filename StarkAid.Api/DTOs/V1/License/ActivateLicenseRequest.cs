namespace StarkAid.Api.DTOs.V1.License;

public class ActivateLicenseRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string? MachineName { get; set; }
}

