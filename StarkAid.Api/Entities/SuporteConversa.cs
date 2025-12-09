using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class SuporteConversa
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Origem { get; set; } = "software"; // "software" ou "app"

    [MaxLength(1000)]
    public string? ProblemaInicial { get; set; }

    public string? Mensagens { get; set; } // JSON array de mensagens

    public int ContadorMensagens { get; set; } = 0; // Contador de mensagens após IA entrar em ação

    public bool ChatConcluido { get; set; } = false;

    public bool Resolvido { get; set; } = false;

    public bool LimiteAtingido { get; set; } = false;

    public bool TransferidoParaHumano { get; set; } = false;

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ConcluidoEm { get; set; }
}
