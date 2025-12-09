namespace StarkAid.Api.DTOs.License;

public class LicenseActivationDto
{
    public Guid Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public DateTimeOffset ActivatedAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? IpAddress { get; set; }
}

