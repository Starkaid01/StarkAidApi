using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class EwelinkAccount
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(500)]
    public string AccessToken { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string RefreshToken { get; set; } = string.Empty;

    public long AccessTokenExpiry { get; set; }

    public long RefreshTokenExpiry { get; set; }

    [MaxLength(50)]
    public string? Region { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastUpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
