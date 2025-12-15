using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// NÚCLEO DA INTELIGÊNCIA DO SISTEMA
/// Gerencia o ciclo de reforço baseado em erros
/// 
/// REGRAS:
/// 1. Toda vez que o usuário errar uma questão → entra aqui
/// 2. Cada novo erro incrementa Tentativas
/// 3. Cada acerto posterior incrementa AcertosAposErro
/// 4. Quando AcertosAposErro >= 3 → remover do ciclo de reforço
/// 5. Questões com mais erros e menos acertos têm prioridade máxima
/// </summary>
[Table("QuestaoErro")]
public class QuestaoErro
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public int QuestaoId { get; set; }

    [Required]
    public int Tentativas { get; set; } = 1; // Quantas vezes errou

    [Required]
    public int AcertosAposErro { get; set; } = 0; // Quantas vezes acertou após errar

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataPrimeiroErro { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataUltimoErro { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? DataUltimoAcerto { get; set; }

    [Required]
    public bool EmReforco { get; set; } = true; // Se está no ciclo de reforço

    [Required]
    public int Prioridade { get; set; } = 100; // Calculado: (Tentativas * 10) - (AcertosAposErro * 3)

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

    // Navegações
    [ForeignKey(nameof(UsuarioId))]
    public UsuarioDetran Usuario { get; set; } = null!;

    [ForeignKey(nameof(QuestaoId))]
    public Questao Questao { get; set; } = null!;
}
