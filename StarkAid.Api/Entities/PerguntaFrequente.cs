using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class PerguntaFrequente
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Pergunta { get; set; } = string.Empty;

    [Required]
    public string Resposta { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? SuporteToSoft { get; set; } // Ex: "suporteToSoft:limparCache"

    [MaxLength(200)]
    public string? SuporteToApp { get; set; } // Ex: "suporteToApp:limparCache"

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool Resolvido { get; set; } = false;
}
