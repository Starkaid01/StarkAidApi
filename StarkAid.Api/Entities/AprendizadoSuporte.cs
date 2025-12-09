using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class AprendizadoSuporte
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string Problema { get; set; } = string.Empty; // Descrição do problema

    [Required, MaxLength(500)]
    public string Solucao { get; set; } = string.Empty; // Solução que funcionou

    [MaxLength(200)]
    public string? ComandoSoft { get; set; } // Comando para software

    [MaxLength(200)]
    public string? ComandoApp { get; set; } // Comando para app

    [MaxLength(50)]
    public string? Origem { get; set; } // "software" ou "app"

    public int ContadorSucesso { get; set; } = 1; // Quantas vezes funcionou

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastUsedAt { get; set; }
}
