using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class LogFalhaSoft
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string TipoFalha { get; set; } = string.Empty; // "DispositivoNaoAcionado", "StarkCoinsInsuficiente", "ErroComando", etc.

    [MaxLength(1000)]
    public string? Descricao { get; set; }

    [MaxLength(500)]
    public string? ComandoTentado { get; set; }

    [MaxLength(500)]
    public string? DispositivoNome { get; set; }

    [MaxLength(200)]
    public string? ErroDetalhado { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

