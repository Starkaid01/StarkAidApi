using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public class FirebaseToken
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [ForeignKey(nameof(User))] // <- ESSENCIAL PARA REMOVER O USERID1
    public Guid UserId { get; set; }

    [Required]
    public string Token { get; set; } = null!;

    [Required]
    public DateTimeOffset DataCadastro { get; set; }

    public User User { get; set; } = null!;
}