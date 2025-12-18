using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Data;
using StarkAid.Api.DTOs.V1.Users;
using StarkAid.Api.Entities;
using System.Security.Claims;

namespace StarkAid.Api.Controllers.V1;

[ApiVersion("1.0")]
[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class StarkCoinsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<StarkCoinsController> _logger;

    public StarkCoinsController(AppDbContext db, ILogger<StarkCoinsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] StarkCoinPurchaseRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        var (amount, price) = request.PackageType switch
        {
            StarkCoinPackageType.Pack5 => (5, 4.90m),
            StarkCoinPackageType.Pack15 => (15, 9.90m),
            StarkCoinPackageType.Pack50 => (50, 19.90m),
            StarkCoinPackageType.Pack120 => (120, 39.90m),
            _ => (0, 0m)
        };

        if (amount == 0)
            return BadRequest("Pacote inválido.");

        var purchase = new StarkCoinPurchase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PackageType = request.PackageType,
            StarkCoinsAmount = amount,
            Price = price,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.StarkCoins += amount;

        _db.StarkCoinPurchases.Add(purchase);
        await _db.SaveChangesAsync();

        _logger.LogInformation("StarkCoins adicionadas para usuário {UserId}: +{Amount} (pacote {Package})", userId, amount, request.PackageType);

        return Ok(new
        {
            purchase.Id,
            purchase.PackageType,
            purchase.StarkCoinsAmount,
            purchase.Price,
            user.StarkCoins
        });
    }
}

