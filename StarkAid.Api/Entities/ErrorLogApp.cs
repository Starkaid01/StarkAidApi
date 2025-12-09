using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class ErrorLogApp
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public string? UltimoComando { get; set; }
    public string? UltimaResposta { get; set; }
    public string? UltimoDispositivoAcionado { get; set; }
    public string? ErroCompleto { get; set; }
    public string? CodigoDeErro { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string DataErro { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string HoraErro { get; set; } = string.Empty;
    
    [Required]
    public string AcaoErro { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

