using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class ComandoSocial
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Comando { get; set; } = string.Empty;

    [Required]
    public string Resposta { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}