using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Acompanha o progresso geral do estudante e métricas de prontidão
/// </summary>
[Table("ProgressoEstudo")]
public class ProgressoEstudo
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    public int? CategoriaId { get; set; } // NULL = progresso geral

    [Required]
    public int TotalQuestoes { get; set; } = 0;

    [Required]
    public int QuestoesRespondidas { get; set; } = 0;

    [Required]
    public int QuestoesCorretas { get; set; } = 0;

    [Required]
    public int QuestoesErradas { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxaAcerto { get; set; } = 0.00m; // Percentual

    [Required]
    public int QuestoesEmReforco { get; set; } = 0; // Quantas questões estão no ciclo de reforço

    [Required]
    public byte NivelProntidao { get; set; } = 0; // 0-100 (calculado)

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataUltimaAtualizacao { get; set; } = DateTime.UtcNow;

    // Navegações
    [ForeignKey(nameof(UsuarioId))]
    public UsuarioDetran Usuario { get; set; } = null!;

    [ForeignKey(nameof(CategoriaId))]
    public CategoriaQuestao? Categoria { get; set; }
}
