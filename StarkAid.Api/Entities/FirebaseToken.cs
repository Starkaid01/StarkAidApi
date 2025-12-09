using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class FirebaseToken
{
    [Key] public Guid Id { get; set; }

    [Required, ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    [Required] public string Token { get; set; } = string.Empty;

    [Required] public DateTimeOffset DataCadastro { get; set; }

    public User User { get; set; } = null!;
}
