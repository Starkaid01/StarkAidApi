using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class LicenseActivation
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid LicenseId { get; set; }

    public License? License { get; set; }

    [Required, MaxLength(200)]
    public string MachineId { get; set; } = string.Empty; // Identificador único da máquina

    [MaxLength(200)]
    public string? MachineName { get; set; } // Nome da máquina (opcional)

    [Required]
    public DateTimeOffset ActivatedAt { get; set; }

    public DateTimeOffset? DeactivatedAt { get; set; }

    [Required]
    public bool IsActive { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; } // IP da máquina no momento da ativação
}

