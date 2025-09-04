using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Entities;

namespace StarkAid.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<ComandoSocial> ComandosSociais => Set<ComandoSocial>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<FirebaseToken> FirebaseTokens => Set<FirebaseToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<DispositivoDisparo> DispositivosDisparo => Set<DispositivoDisparo>();
    public DbSet<Disparo> Disparos => Set<Disparo>();
    public DbSet<WebhookLog> WebhookLogs => Set<WebhookLog>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Assinatura
        modelBuilder.Entity<Assinatura>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.StripeCustomerId).HasMaxLength(100);
            entity.Property(a => a.StripeSubscriptionId).HasMaxLength(100);
            entity.Property(a => a.Status).HasMaxLength(50);
            entity.Property(a => a.Valor).HasColumnType("decimal(18,2)");
            entity.Property(a => a.IniciadaEm).HasColumnType("datetimeoffset");
            entity.Property(a => a.CanceladaEm).HasColumnType("datetimeoffset");
            entity.Property(a => a.ExpiraEm).HasColumnType("datetimeoffset");
            entity.Property(a => a.PagamentoConfirmadoEm).HasColumnType("datetimeoffset");
            entity.Property(a => a.DataCriacao).HasColumnType("datetimeoffset").IsRequired();

            
            entity.HasOne(a => a.User)
                .WithMany(u => u.Assinaturas)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Name).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.ApiKey).IsRequired().HasMaxLength(100);
            entity.Property(u => u.StarkCoins).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(u => u.IsActive).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired();
            entity.Property(u => u.PreapprovalId).HasMaxLength(100);
            entity.Property(u => u.UltimoPagamentoConfirmadoEm).HasColumnType("datetimeoffset");

            entity.Property(u => u.SpotifyAccessToken).HasMaxLength(500);
            entity.Property(u => u.SpotifyRefreshToken).HasMaxLength(500);
            entity.Property(u => u.SpotifyTokenExpiresAt).HasColumnType("datetimeoffset");

            entity.HasMany(u => u.RefreshTokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.PasswordResetTokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.FirebaseTokens)
                .WithOne(f => f.User)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Devices)
                .WithOne(d => d.User)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.ComandosSociais)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Agendamentos)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.DispositivosDisparo)
                .WithOne(d => d.User)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Disparos)
                .WithOne(d => d.User)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Token).IsRequired();
            entity.Property(t => t.Expiration).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(t => t.IsRevoked).IsRequired();
        });

        // PasswordResetToken
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Token).IsRequired();
            entity.Property(t => t.Expiration).HasColumnType("datetimeoffset").IsRequired();
        });

        // FirebaseToken
        modelBuilder.Entity<FirebaseToken>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Token).IsRequired();
            entity.Property(f => f.DataCadastro).HasColumnType("datetimeoffset").IsRequired();
        });

        // Device
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(150);
            entity.Property(d => d.ApiKey).IsRequired().HasMaxLength(100);
            entity.Property(d => d.MqttTopic).IsRequired().HasMaxLength(200);

            entity.HasMany(d => d.Agendamentos)
                .WithOne(a => a.Device)
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Agendamento
        modelBuilder.Entity<Agendamento>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AgendadoPara).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(a => a.Comando).IsRequired();
            entity.Property(a => a.Executado).IsRequired();
        });

        // DispositivoDisparo
        modelBuilder.Entity<DispositivoDisparo>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Nome).IsRequired().HasMaxLength(150);
            entity.Property(d => d.MqttTopic).IsRequired().HasMaxLength(200);
            entity.Property(d => d.StatusTopic).IsRequired().HasMaxLength(200);
            entity.Property(d => d.DataCadastro).HasColumnType("datetimeoffset").IsRequired();
        });

        // Disparo
        modelBuilder.Entity<Disparo>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.DisparadoEm).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(p => p.Mensagem).IsRequired();
            entity.Property(p => p.Confirmado).IsRequired();
            entity.Property(p => p.ConfirmadoEm).HasColumnType("datetimeoffset");

            entity.HasOne(p => p.Dispositivo)
                .WithMany(d => d.Disparos)
                .HasForeignKey(p => p.DispositivoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.User)
                .WithMany(u => u.Disparos)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ComandoSocial
        modelBuilder.Entity<ComandoSocial>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Comando).IsRequired();
            entity.Property(c => c.Resposta).IsRequired();
        });
    }
}
