namespace StarkAid.Api.DTOs.License;

public class ActivateLicenseRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string? MachineName { get; set; }
}

