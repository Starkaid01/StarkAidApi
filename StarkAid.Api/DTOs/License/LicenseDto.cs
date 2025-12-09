namespace StarkAid.Api.DTOs.License;

public class LicenseDto
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
    public List<LicenseActivationDto> Activations { get; set; } = new();
}

