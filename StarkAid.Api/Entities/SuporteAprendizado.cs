using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class SuporteAprendizado
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Problema { get; set; } = string.Empty;

    [Required]
    public string Solucoes { get; set; } = string.Empty; // JSON array de soluções que funcionaram

    [Required]
    [MaxLength(20)]
    public string Origem { get; set; } = "software"; // "software" ou "app"

    public int ContadorSucesso { get; set; } = 1; // Quantas vezes essa solução funcionou

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastUsedAt { get; set; }
}
