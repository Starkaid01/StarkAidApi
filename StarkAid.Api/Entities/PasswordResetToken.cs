using StarkAid.Api.Entities;
using System.ComponentModel.DataAnnotations;

public class PasswordResetToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Token { get; set; } = null!;

    [Required]
    public DateTimeOffset Expiration { get; set; } // datetimeoffset no DbContext

    public User User { get; set; } = null!;
}