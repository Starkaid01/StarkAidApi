using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class EwelinkDevice
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int Type { get; set; }

    public int Uiid { get; set; }

    public string? Params { get; set; } // JSON serializado

    public bool Online { get; set; }

    [MaxLength(100)]
    public string? FamilyId { get; set; }

    [MaxLength(100)]
    public string? RoomId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastUpdatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
