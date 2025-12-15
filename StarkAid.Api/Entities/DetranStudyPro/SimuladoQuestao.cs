using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Relacionamento entre Simulado e Questões
/// Armazena quais questões foram selecionadas para cada simulado
/// </summary>
[Table("SimuladoQuestao")]
public class SimuladoQuestao
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SimuladoId { get; set; }

    [Required]
    public int QuestaoId { get; set; }

    [Required]
    public int Ordem { get; set; } // Ordem da questão no simulado

    // Navegações
    [ForeignKey(nameof(SimuladoId))]
    public Simulado Simulado { get; set; } = null!;

    [ForeignKey(nameof(QuestaoId))]
    public Questao Questao { get; set; } = null!;
}
