using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class License
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public User? User { get; set; }

    [Required, MaxLength(100)]
    public string LicenseKey { get; set; } = string.Empty;

    [Required]
    public int MaxMachines { get; set; } // 2 ou 4 máquinas

    [Required]
    public decimal Price { get; set; } // 250.00 ou 454.00

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset ExpiresAt { get; set; } // Licença vitalícia ou com expiração

    [Required]
    public bool IsActive { get; set; }

    [MaxLength(100)]
    public string? StripeSessionId { get; set; }

    [MaxLength(100)]
    public string? StripePaymentIntentId { get; set; }

    public DateTimeOffset? PaymentConfirmedAt { get; set; }

    // Navegação
    public ICollection<LicenseActivation> Activations { get; set; } = new List<LicenseActivation>();
}

