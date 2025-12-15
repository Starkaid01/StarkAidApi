using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Registra o tempo de estudo diário do estudante
/// </summary>
[Table("TempoEstudo")]
public class TempoEstudo
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    [Column(TypeName = "date")]
    public DateTime Data { get; set; } // Data do estudo (sem hora)

    [Required]
    public int TempoMinutos { get; set; } = 0; // Tempo total em minutos

    [Required]
    public int QuestoesRespondidas { get; set; } = 0;

    [Required]
    public int Sessoes { get; set; } = 1; // Quantas sessões de estudo no dia

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

    // Navegações
    [ForeignKey(nameof(UsuarioId))]
    public UsuarioDetran Usuario { get; set; } = null!;
}
