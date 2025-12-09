using System;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class RefreshToken
{
    [Key] public Guid Id { get; set; }

    [Required] public string Token { get; set; } = string.Empty;

    [Required] public DateTimeOffset Expiration { get; set; }

    [Required] public bool IsRevoked { get; set; }

    [Required] public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Origem { get; set; } = "web"; // "app" ou "web"
}
