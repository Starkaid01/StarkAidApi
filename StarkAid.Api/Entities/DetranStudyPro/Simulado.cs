using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Simulados realizados pelo estudante
/// </summary>
[Table("Simulado")]
public class Simulado
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Tipo { get; set; } = string.Empty; // 'completo', 'categoria', 'reforco', 'aleatorio'

    public int? CategoriaId { get; set; } // Se for simulado por categoria

    [Required]
    public int TotalQuestoes { get; set; }

    [Required]
    public int QuestoesCorretas { get; set; } = 0;

    [Required]
    public int QuestoesErradas { get; set; } = 0;

    public int? TempoTotal { get; set; } // Tempo total em segundos

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Nota { get; set; } // Nota de 0 a 100

    public bool? Aprovado { get; set; } // NULL = em andamento, TRUE = aprovado, FALSE = reprovado

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataInicio { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? DataFim { get; set; }

    [Required]
    public bool Concluido { get; set; } = false;

    // Navegações
    [ForeignKey(nameof(UsuarioId))]
    public UsuarioDetran Usuario { get; set; } = null!;

    [ForeignKey(nameof(CategoriaId))]
    public CategoriaQuestao? Categoria { get; set; }

    public ICollection<RespostaEstudante> Respostas { get; set; } = new List<RespostaEstudante>();
}
