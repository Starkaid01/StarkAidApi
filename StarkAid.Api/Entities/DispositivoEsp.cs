using System;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class DispositivoEsp
{
    [Key] 
    public Guid Id { get; set; }

    [Required, MaxLength(150)] 
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(45)] 
    public string Ip { get; set; } = string.Empty;

    [Required] 
    public int Porta { get; set; }

    [MaxLength(200)]
    public string? Comando { get; set; }

    [MaxLength(200)]
    public string? ComandToEsp { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Desconectado";

    public bool LigadoDesligado { get; set; } = false;

    public Guid? UserId { get; set; }

    [Required] 
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastPingAt { get; set; }

    public DateTimeOffset? LastUpdatedAt { get; set; }

    public User? User { get; set; }
}

