using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class SuportePerguntaFrequente
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Pergunta { get; set; } = string.Empty;

    [Required]
    public string Resposta { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? SuporteToSoft { get; set; } // Comando para Windows Forms (ex: "limparcache")

    [MaxLength(200)]
    public string? SuporteToApp { get; set; } // Comando para App (ex: "limparcache")

    public bool RequerAcao { get; set; } = false; // Se precisa executar ação no cliente

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastUpdatedAt { get; set; }
}
