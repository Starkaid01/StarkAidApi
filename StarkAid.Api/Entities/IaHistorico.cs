using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class IaHistorico
{
    [Key] public Guid Id { get; set; }

    [Required] public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required] public string TextoUsuario { get; set; } = string.Empty;

    [Required] public string TextoIa { get; set; } = string.Empty;

    [Required] public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
}
