using System;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class PasswordResetToken
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid UserId { get; set; }

    [Required] public string Token { get; set; } = string.Empty;

    [Required] public DateTimeOffset Expiration { get; set; }

    public User User { get; set; } = null!;
}
