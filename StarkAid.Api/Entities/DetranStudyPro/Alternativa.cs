using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Alternativas de cada questão (A, B, C, D)
/// </summary>
[Table("Alternativa")]
public class Alternativa
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int QuestaoId { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Texto { get; set; } = string.Empty;

    [Required]
    [MaxLength(1)]
    public string Letra { get; set; } = string.Empty; // A, B, C, D

    [Required]
    public bool Correta { get; set; } = false;

    [Required]
    public byte Ordem { get; set; } // 1, 2, 3, 4

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Navegações
    [ForeignKey(nameof(QuestaoId))]
    public Questao Questao { get; set; } = null!;

    public ICollection<RespostaEstudante> Respostas { get; set; } = new List<RespostaEstudante>();
}
