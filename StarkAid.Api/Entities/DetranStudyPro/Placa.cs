using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Placas de trânsito - relacionada com as imagens baixadas
/// </summary>
[Table("Placa")]
public class Placa
{

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty; // Ex: R-15, A-1a

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descricao { get; set; }

    [Required]
    public int CategoriaId { get; set; }

    [MaxLength(500)]
    public string? CaminhoImagem { get; set; } // Caminho relativo da imagem

    [MaxLength(500)]
    public string? Significado { get; set; }

    [MaxLength(500)]
    public string? QuandoUsar { get; set; }

    [MaxLength(500)]
    public string? Dica { get; set; }

    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Navegações
    [ForeignKey(nameof(CategoriaId))]
    public CategoriaQuestao Categoria { get; set; } = null!;

    public ICollection<Questao> Questoes { get; set; } = new List<Questao>();
}
