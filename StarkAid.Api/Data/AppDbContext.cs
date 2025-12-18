using Microsoft.EntityFrameworkCore;
using StarkAid.Api.Entities;
using StarkAid.Api.Options;
using System;

namespace StarkAid.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets
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
    public DbSet<IaHistorico> IaHistoricos => Set<IaHistorico>();
    public DbSet<ConfiguracaoSistema> ConfiguracoesSistema => Set<ConfiguracaoSistema>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<PagamentoAvulso> PagamentosAvulsos => Set<PagamentoAvulso>();
    public DbSet<ConfiguracaoStarkNlp> ConfiguracoesStarkNlp => Set<ConfiguracaoStarkNlp>();
    public DbSet<DispositivoEsp> DispositivosEsp => Set<DispositivoEsp>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<LicenseActivation> LicenseActivations => Set<LicenseActivation>();
    public DbSet<EwelinkAccount> EwelinkAccounts => Set<EwelinkAccount>();
    public DbSet<EwelinkDevice> EwelinkDevices => Set<EwelinkDevice>();
    public DbSet<ErrorLogSoft> ErrorLogsSoft => Set<ErrorLogSoft>();
    public DbSet<ErrorLogApp> ErrorLogsApp => Set<ErrorLogApp>();
    public DbSet<ErrorCodeDescription> ErrorCodeDescriptions => Set<ErrorCodeDescription>();
    public DbSet<UserActivity> UserActivities => Set<UserActivity>();
    public DbSet<LogFalhaSoft> LogsFalhasSoft => Set<LogFalhaSoft>();
    public DbSet<SuporteAprendizado> SuporteAprendizados => Set<SuporteAprendizado>();
    public DbSet<SuportePerguntaFrequente> SuportePerguntasFrequentes => Set<SuportePerguntaFrequente>();
    public DbSet<SuporteConversa> SuporteConversas => Set<SuporteConversa>();
    public DbSet<SuporteAcao> SuporteAcoes => Set<SuporteAcao>();
    public DbSet<ResolvendoSuporte> ResolvendoSuportes => Set<ResolvendoSuporte>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<StarkCoinPurchase> StarkCoinPurchases => Set<StarkCoinPurchase>();

  

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações de IA Historico
        modelBuilder.Entity<IaHistorico>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TextoUsuario).IsRequired();
            entity.Property(e => e.TextoIa).IsRequired();
            entity.Property(e => e.CriadoEm).HasColumnType("datetimeoffset").IsRequired();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de Assinatura
        modelBuilder.Entity<Assinatura>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.StripeCustomerId).HasMaxLength(100);
            entity.Property(a => a.StripeSubscriptionId).HasMaxLength(100);
            entity.Property(a => a.StripePriceId).HasMaxLength(100);
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

        // Configurações de User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.ApiKey).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PlanType).HasConversion<int>().HasDefaultValue(UserPlanType.Free).IsRequired();
            entity.Property(u => u.StarkCoins)
              .IsRequired()
              .ValueGeneratedNever();

            entity.Property(u => u.TokensConsumidosSemana)
              .IsRequired()
              .ValueGeneratedNever();

            entity.Property(u => u.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(u => u.IsActive).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired();
            entity.Property(u => u.PreapprovalId).HasMaxLength(100);
            entity.Property(u => u.UltimoPagamentoConfirmadoEm).HasColumnType("datetimeoffset");
            entity.Property(u => u.SpotifyAccessToken).HasMaxLength(500);
            entity.Property(u => u.SpotifyRefreshToken).HasMaxLength(500);
            entity.Property(u => u.SpotifyTokenExpiresAt).HasColumnType("datetimeoffset");
            entity.Property(u => u.MinutosReconhecidos).HasColumnType("float").IsRequired().HasDefaultValue(0);
        });

        // Configurações de RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Token).IsRequired();
            entity.Property(rt => rt.Expiration).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(rt => rt.IsRevoked).IsRequired();
        });

        // Configurações de PasswordResetToken
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(prt => prt.Id);
            entity.Property(prt => prt.Token).IsRequired();
            entity.Property(prt => prt.Expiration).HasColumnType("datetimeoffset").IsRequired();
        });

        // Configurações de Device
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(150);
            entity.Property(d => d.ApiKey).IsRequired().HasMaxLength(100);
            entity.Property(d => d.MqttTopic).IsRequired().HasMaxLength(200);
        });

        // Configurações de Agendamento
        modelBuilder.Entity<Agendamento>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.AgendadoPara).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(a => a.Comando).IsRequired();
            entity.Property(a => a.Executado).IsRequired();
            entity.Property(a => a.TipoAgendamento).IsRequired();
            
            entity.HasOne(a => a.Device)
                  .WithMany()
                  .HasForeignKey(a => a.DeviceId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasOne(a => a.DispositivoEsp)
                  .WithMany()
                  .HasForeignKey(a => a.DispositivoEspId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configurações de DispositivoDisparo
        modelBuilder.Entity<DispositivoDisparo>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Nome).IsRequired().HasMaxLength(150);
            entity.Property(d => d.MqttTopic).IsRequired().HasMaxLength(200);
            entity.Property(d => d.StatusTopic).IsRequired().HasMaxLength(200);
            entity.Property(d => d.DataCadastro).HasColumnType("datetimeoffset").IsRequired();
        });

        // Configurações de Disparo
        modelBuilder.Entity<Disparo>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DisparadoEm).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(d => d.Mensagem).IsRequired();
            entity.Property(d => d.Confirmado).IsRequired();
            entity.Property(d => d.ConfirmadoEm).HasColumnType("datetimeoffset");
            entity.HasOne(d => d.Dispositivo)
                  .WithMany(dd => dd.Disparos)
                  .HasForeignKey(d => d.DispositivoId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.User)
                  .WithMany(u => u.Disparos)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configurações de ComandoSocial
        modelBuilder.Entity<ComandoSocial>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Comando).IsRequired();
            entity.Property(c => c.Resposta).IsRequired();
            entity.Property(c => c.RespostasAleatorias).HasColumnType("nvarchar(max)");
        });

        // Configurações de DispositivoEsp
        modelBuilder.Entity<DispositivoEsp>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Nome).IsRequired().HasMaxLength(150);
            entity.Property(d => d.Ip).IsRequired().HasMaxLength(45);
            entity.Property(d => d.Porta).IsRequired();
            entity.Property(d => d.Status).IsRequired().HasMaxLength(50);
            entity.Property(d => d.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(d => d.LastPingAt).HasColumnType("datetimeoffset");
            entity.Property(d => d.LastUpdatedAt).HasColumnType("datetimeoffset");
            
            entity.HasOne(d => d.User)
                  .WithMany()
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configurações de License
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.LicenseKey).IsRequired().HasMaxLength(100);
            entity.Property(l => l.MaxMachines).IsRequired();
            entity.Property(l => l.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(l => l.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(l => l.ExpiresAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(l => l.IsActive).IsRequired();
            entity.Property(l => l.StripeSessionId).HasMaxLength(100);
            entity.Property(l => l.StripePaymentIntentId).HasMaxLength(100);
            entity.Property(l => l.PaymentConfirmedAt).HasColumnType("datetimeoffset");

            entity.HasOne(l => l.User)
                  .WithMany()
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de LicenseActivation
        modelBuilder.Entity<LicenseActivation>(entity =>
        {
            entity.HasKey(la => la.Id);
            entity.Property(la => la.MachineId).IsRequired().HasMaxLength(200);
            entity.Property(la => la.MachineName).HasMaxLength(200);
            entity.Property(la => la.ActivatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(la => la.DeactivatedAt).HasColumnType("datetimeoffset");
            entity.Property(la => la.IsActive).IsRequired();
            entity.Property(la => la.IpAddress).HasMaxLength(50);

            entity.HasOne(la => la.License)
                  .WithMany(l => l.Activations)
                  .HasForeignKey(la => la.LicenseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de EwelinkAccount
        modelBuilder.Entity<EwelinkAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccessToken).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RefreshToken).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Region).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(e => e.LastUpdatedAt).HasColumnType("datetimeoffset");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de EwelinkDevice
        modelBuilder.Entity<EwelinkDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FamilyId).HasMaxLength(100);
            entity.Property(e => e.RoomId).HasMaxLength(100);
            entity.Property(e => e.Params).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(e => e.LastUpdatedAt).HasColumnType("datetimeoffset");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de UserSession
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(us => us.Id);
            entity.Property(us => us.SessionName).IsRequired().HasMaxLength(100);
            entity.Property(us => us.Token).IsRequired();
            entity.Property(us => us.Origem).IsRequired().HasMaxLength(50);
            entity.Property(us => us.CreatedAt).IsRequired();
            entity.Property(us => us.LastActivityAt);
            
            entity.HasOne(us => us.User)
                  .WithMany()
                  .HasForeignKey(us => us.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StarkCoinPurchase>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.PackageType).IsRequired();
            entity.Property(p => p.StarkCoinsAmount).IsRequired();
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(p => p.CreatedAt).HasColumnType("datetimeoffset").IsRequired();

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de ErrorLogSoft
        modelBuilder.Entity<ErrorLogSoft>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.DataErro).IsRequired().HasMaxLength(50);
            entity.Property(e => e.HoraErro).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AcaoErro).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de ErrorLogApp
        modelBuilder.Entity<ErrorLogApp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.DataErro).IsRequired().HasMaxLength(50);
            entity.Property(e => e.HoraErro).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AcaoErro).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de ErrorCodeDescription
        modelBuilder.Entity<ErrorCodeDescription>(entity =>
        {
            entity.HasKey(e => e.CodigoDeErro);
            entity.Property(e => e.CodigoDeErro).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Descricao).IsRequired();
            entity.Property(e => e.Contexto).IsRequired();
            entity.Property(e => e.CamposRelevantes).IsRequired();
            entity.Property(e => e.Origem).HasMaxLength(20);
        });

        // Configurações de SuporteConversa
        modelBuilder.Entity<SuporteConversa>(entity =>
        {
            entity.ToTable("SuporteConversas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Origem).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ProblemaInicial).HasMaxLength(1000);
            entity.Property(e => e.ContadorMensagens).IsRequired();
            entity.Property(e => e.ChatConcluido).IsRequired();
            entity.Property(e => e.Resolvido).IsRequired();
            entity.Property(e => e.LimiteAtingido).IsRequired();
            entity.Property(e => e.TransferidoParaHumano).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(e => e.ConcluidoEm).HasColumnType("datetimeoffset");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de SuporteAprendizado
        modelBuilder.Entity<SuporteAprendizado>(entity =>
        {
            entity.ToTable("SuporteAprendizados");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Problema).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Solucoes).IsRequired();
            entity.Property(e => e.Origem).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ContadorSucesso).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(e => e.LastUsedAt).HasColumnType("datetimeoffset");
        });

        // Configurações de SuportePerguntaFrequente
        modelBuilder.Entity<SuportePerguntaFrequente>(entity =>
        {
            entity.ToTable("SuportePerguntasFrequentes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Pergunta).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Resposta).IsRequired();
            entity.Property(e => e.SuporteToSoft).HasMaxLength(200);
            entity.Property(e => e.SuporteToApp).HasMaxLength(200);
            entity.Property(e => e.RequerAcao).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(e => e.LastUpdatedAt).HasColumnType("datetimeoffset");
        });

        // Configurações de SuporteAcao
        modelBuilder.Entity<SuporteAcao>(entity =>
        {
            entity.ToTable("SuporteAcoes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Origem).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Acao).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Resposta).HasMaxLength(500);
            entity.Property(e => e.Sucesso).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
        });

        // Configurações de ResolvendoSuporte
        modelBuilder.Entity<ResolvendoSuporte>(entity =>
        {
            entity.ToTable("ResolvendoSuportes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Origem).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Ativo).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(e => e.ResolvidoEm).HasColumnType("datetimeoffset");
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Mensagem).IsRequired();
            entity.Property(e => e.Lida).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(e => e.LidaEm).HasColumnType("datetimeoffset");
            entity.Property(e => e.Valor).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UserEmail).HasMaxLength(200);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.ReferenciaId).HasMaxLength(100);
        });

        // Configurações de LogFalhaSoft
        modelBuilder.Entity<LogFalhaSoft>(entity =>
        {
            entity.ToTable("LogsFalhasSoft");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.TipoFalha).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.Property(e => e.ComandoTentado).HasMaxLength(500);
            entity.Property(e => e.DispositivoNome).HasMaxLength(500);
            entity.Property(e => e.ErroDetalhado).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
       
    }
}
