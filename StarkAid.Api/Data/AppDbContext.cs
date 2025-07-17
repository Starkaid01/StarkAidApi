using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Entities;
using StarkAid.Api.EntityConfigurations;

namespace StarkAid.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<ComandoSocial> ComandosSociais { get; set; }
    public DbSet<Agendamento> Agendamentos { get; set; }
    public DbSet<FirebaseToken> FirebaseTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<DispositivoDisparo> DispositivosDisparo { get; set; }
    public DbSet<Disparo> Disparos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new FirebaseTokenConfiguration());

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId);

        modelBuilder.Entity<Agendamento>()
            .Property(a => a.AgendadoPara)
            .HasColumnType("timestamp with time zone");
    }
}
