using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Registra todas as respostas do estudante (histórico completo)
/// </summary>
[Table("RespostaEstudante")]
public class RespostaEstudante
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public int QuestaoId { get; set; }

    [Required]
    public int AlternativaId { get; set; } // Alternativa escolhida

    [Required]
    public bool Correta { get; set; } // Se a resposta estava correta

    public int? TempoResposta { get; set; } // Tempo em segundos para responder

    [MaxLength(50)]
    public string? Contexto { get; set; } // 'estudo', 'simulado', 'reforco', 'revisao'

    public int? SimuladoId { get; set; } // Se foi respondida em um simulado

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataResposta { get; set; } = DateTime.UtcNow;

    // Navegações
    [ForeignKey(nameof(UsuarioId))]
    public UsuarioDetran Usuario { get; set; } = null!;

    [ForeignKey(nameof(QuestaoId))]
    public Questao Questao { get; set; } = null!;

    [ForeignKey(nameof(AlternativaId))]
    public Alternativa Alternativa { get; set; } = null!;

    [ForeignKey(nameof(SimuladoId))]
    public Simulado? Simulado { get; set; }
}
