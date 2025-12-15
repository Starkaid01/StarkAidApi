using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Banco completo de questões da prova do DETRAN-MG
/// </summary>
[Table("Questao")]
public class Questao
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string Enunciado { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImagemUrl { get; set; } // URL ou caminho da imagem

    [Required]
    public int CategoriaId { get; set; }

    public int? PlacaId { get; set; } // Se a questão é sobre uma placa específica

    [Required]
    public byte Dificuldade { get; set; } = 2; // 1=Fácil, 2=Médio, 3=Difícil

    [MaxLength(100)]
    public string? Fonte { get; set; } // Fonte da questão (ex: "DETRAN-MG 2024")

    public int? Ano { get; set; } // Ano da questão original

    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? DataAtualizacao { get; set; }

    // Navegações
    [ForeignKey(nameof(CategoriaId))]
    public CategoriaQuestao Categoria { get; set; } = null!;

    [ForeignKey(nameof(PlacaId))]
    public Placa? Placa { get; set; }

    public ICollection<Alternativa> Alternativas { get; set; } = new List<Alternativa>();
    public ICollection<RespostaEstudante> Respostas { get; set; } = new List<RespostaEstudante>();
    public ICollection<QuestaoErro> QuestoesErro { get; set; } = new List<QuestaoErro>();
}
