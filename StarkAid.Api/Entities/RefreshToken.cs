using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }

    public string Token { get; set; } = null!;
    public DateTime Expiration { get; set; }
    public bool IsRevoked { get; set; }

    // Relacionamento com User
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}