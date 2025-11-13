using StarkAid.Api.Entities;
using System.ComponentModel.DataAnnotations;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Token { get; set; } = null!;

    [Required]
    public DateTimeOffset Expiration { get; set; } // datetimeoffset no DbContext

    [Required]
    public bool IsRevoked { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    // 🔥 Novo campo
    public string Origem { get; set; } = "web"; // default "web"
}