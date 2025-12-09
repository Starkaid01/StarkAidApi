using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class UserSession
{
    [Key] public int Id { get; set; }

    [Required] public Guid UserId { get; set; }

    [Required, MaxLength(100)] public string SessionName { get; set; } = string.Empty;

    [Required] public string Token { get; set; } = string.Empty;

    [Required, MaxLength(50)] public string Origem { get; set; } = string.Empty; // web, soft, app

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastActivityAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
