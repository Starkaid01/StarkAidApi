using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class UserActivity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Origem { get; set; } = string.Empty; // "soft" ou "app"

    // Últimos comandos
    public string? UltimoComandoEsp { get; set; }
    public string? UltimoComandoEwelink { get; set; }
    public string? UltimoComandoStarkSwitch { get; set; }
    public string? UltimoComandoSocial { get; set; }
    public string? UltimaRespostaSocial { get; set; }
    public string? UltimoComandoIA { get; set; }
    public string? UltimaRespostaIA { get; set; }

    [Required]
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

