using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities.DetranStudyPro;

/// <summary>
/// Usuário do sistema DetranStudyPro
/// Separado da tabela principal de usuários do StarkAid
/// </summary>
[Table("Usuario")]
public class UsuarioDetran
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string SenhaHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? DataUltimoAcesso { get; set; }

    [Required]
    public bool Ativo { get; set; } = true;

    [Required]
    public bool EmailConfirmado { get; set; } = false;

    [MaxLength(255)]
    public string? TokenConfirmacao { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? DataExpiracaoToken { get; set; }

    // Navegações
    public ICollection<RespostaEstudante> Respostas { get; set; } = new List<RespostaEstudante>();
    public ICollection<QuestaoErro> QuestoesErro { get; set; } = new List<QuestaoErro>();
    public ICollection<ProgressoEstudo> Progressos { get; set; } = new List<ProgressoEstudo>();
    public ICollection<Simulado> Simulados { get; set; } = new List<Simulado>();
    public ICollection<TempoEstudo> TemposEstudo { get; set; } = new List<TempoEstudo>();
}
