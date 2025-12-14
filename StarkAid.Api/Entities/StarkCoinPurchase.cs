using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public enum StarkCoinPackageType
{
    Pack5 = 0,
    Pack15 = 1,
    Pack50 = 2,
    Pack120 = 3
}

public class StarkCoinPurchase
{
    [Key] public Guid Id { get; set; }

    [Required] public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required] public StarkCoinPackageType PackageType { get; set; }

    [Required] public int StarkCoinsAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Required] public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

