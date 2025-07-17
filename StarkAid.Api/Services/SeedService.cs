using StarkAid.Api.Data;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Services;

public class SeedService
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;

    public SeedService(AppDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public void SeedAdminUser()
    {
        // Verifica se o admin já existe
        if (_context.Users.Any(u => u.Email == "admin@starkaid.com"))
            return;

        // Cria o admin com senha hash via AuthService
        var admin = new User
        {
            //a9fEpeNRN5adCF9v0ZGjMPVwaZBkIauucEkq98V4uUJkV2DmSPEHCNTDEUOmqtzZvCa4wG3blSCNAnoXhDrFRA==
            //enFNl7y6Bn7FnbJmtGBB/qRf6jZsN/bnKQDpFkZditEmIWFUeOYZmNOo/tkGqTObJYG2lZEF9BfPUTeIw4C22A==
            Id = Guid.NewGuid(),
            Name = "Administrador",
            Email = "admin@starkaid.com",
            PasswordHash = _authService.HashPassword("StarkAdmin2024@"),
            ApiKey = Guid.NewGuid().ToString("N"),
            StarkCoins = 0,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = "Administrador"
        };

        _context.Users.Add(admin);
        _context.SaveChanges();
    }
}
