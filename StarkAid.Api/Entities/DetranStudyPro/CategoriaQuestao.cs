using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Categorias das questões (Regulamentação, Advertência, etc.)
/// </summary>
[Table("CategoriaQuestao")]
public class CategoriaQuestao
{

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [Required]
    public int Ordem { get; set; } = 0;

    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Navegações
    public ICollection<Placa> Placas { get; set; } = new List<Placa>();
    public ICollection<Questao> Questoes { get; set; } = new List<Questao>();
    public ICollection<ProgressoEstudo> Progressos { get; set; } = new List<ProgressoEstudo>();
    public ICollection<Simulado> Simulados { get; set; } = new List<Simulado>();
}
