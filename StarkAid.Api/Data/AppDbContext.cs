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
    public DbSet<Aprendizado> Aprendizados => Set<Aprendizado>();
    public DbSet<UserConversaContext> UserConversaContexts => Set<UserConversaContext>();
    public DbSet<GcExecutionLog> GcExecutionLogs => Set<GcExecutionLog>();
    public DbSet<AprendizadoResposta> AprendizadoRespostas => Set<AprendizadoResposta>();
    public DbSet<Telemetria> Telemetrias => Set<Telemetria>();
    public DbSet<AiInteractionEvent> AiInteractionEvents => Set<AiInteractionEvent>();
    public DbSet<SupportLearning> SupportLearnings => Set<SupportLearning>();

    public DbSet<YouTubeMusicCache> YouTubeMusicCaches => Set<YouTubeMusicCache>();
    public DbSet<MusicArtistAlias> MusicArtistAliases => Set<MusicArtistAlias>();

    // Comodos Module
    public DbSet<Comodo> Comodos => Set<Comodo>();
    public DbSet<ComodoDispositivo> ComodoDispositivos => Set<ComodoDispositivo>();
    public DbSet<EscopoConversacional> EscoposConversacionais => Set<EscopoConversacional>();
    
    // Fun Module
    public DbSet<Piada> Piadas => Set<Piada>();
    public DbSet<Receita> Receitas => Set<Receita>();
    public DbSet<ReceitaPasso> ReceitaPassos => Set<ReceitaPasso>();
    public DbSet<UserFunState> UserFunStates => Set<UserFunState>();
    
    // Automation / Routines Module
    public DbSet<Rotina> Rotinas => Set<Rotina>();
    public DbSet<RotinaGatilho> RotinaGatilhos => Set<RotinaGatilho>();
    public DbSet<RotinaAcao> RotinaAcoes => Set<RotinaAcao>();
    public DbSet<Lembrete> Lembretes => Set<Lembrete>();

  

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

            entity.Property(a => a.AgendadoPara)
                .HasColumnType("datetimeoffset")
                .IsRequired();

            entity.Property(a => a.Comando).IsRequired();
            entity.Property(a => a.Executado).IsRequired();
            entity.Property(a => a.TipoAgendamento).IsRequired();

            // 🔥 FIX PRINCIPAL
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict); // ⬅️ ESSENCIAL

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

         modelBuilder.Entity<Aprendizado>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.Texto).IsRequired();
             entity.Property(e => e.Resposta).IsRequired();
             entity.Property(e => e.Contexto).HasMaxLength(500);
             entity.Property(e => e.Tipo).IsRequired().HasMaxLength(50);
             entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
             entity.Property(e => e.UserId).IsRequired(false);
             
             entity.HasIndex(e => new { e.UserId, e.Tipo });
             entity.HasIndex(e => e.Tipo).HasFilter("[Tipo] = 'Global'");
             
             entity.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.SetNull);

             entity.HasMany(e => e.Respostas)
                   .WithOne(r => r.Aprendizado)
                   .HasForeignKey(r => r.AprendizadoId)
                   .OnDelete(DeleteBehavior.Cascade);
         });

         modelBuilder.Entity<AprendizadoResposta>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.Texto).IsRequired();
             entity.Property(e => e.CreatedAt).HasColumnType("datetimeoffset").IsRequired();
         });

         // Configurações de UserConversaContext
         modelBuilder.Entity<UserConversaContext>(entity =>
         {
             entity.HasKey(e => e.UserId);
             entity.Property(e => e.ContextoAtual).HasMaxLength(500);
             entity.Property(e => e.LastUpdatedAt).HasColumnType("datetimeoffset").IsRequired();
         });

         modelBuilder.Entity<Telemetria>(entity =>
         {
             entity.HasKey(t => t.Id);
             entity.Property(t => t.Origem).IsRequired().HasMaxLength(50);
             entity.Property(t => t.Evento).IsRequired().HasMaxLength(100);
             entity.Property(t => t.Categoria).IsRequired().HasMaxLength(50);
             entity.Property(t => t.CriadoEm).HasColumnType("datetimeoffset").IsRequired();
             
             entity.HasOne(t => t.User)
                   .WithMany()
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
         });

         // Configurações de YouTubeMusicCache
         modelBuilder.Entity<YouTubeMusicCache>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.HasIndex(e => e.NormalizedQuery);
             entity.Property(e => e.NormalizedQuery).IsRequired().HasMaxLength(500);
             entity.Property(e => e.VideoId).IsRequired().HasMaxLength(50);
         });

         // --- Fun Module Configurations & Seeding ---

         modelBuilder.Entity<Piada>(entity =>
         {
             entity.HasKey(p => p.Id);
             entity.Property(p => p.Texto).IsRequired();
             
             // Seed 30 Jokes
             entity.HasData(
                new Piada { Id = 1, Texto = "Por que o computador foi ao médico? Porque estava com vírus.", Categoria = "Tecnologia" },
                new Piada { Id = 2, Texto = "O que o zero disse para o oito? Que cinto bonito!", Categoria = "Geral" },
                new Piada { Id = 3, Texto = "Por que o livro de matemática se suicidou? Porque tinha muitos problemas.", Categoria = "Escola" },
                new Piada { Id = 4, Texto = "Qual é o cúmulo da força? Dobrar a esquina.", Categoria = "Geral" },
                new Piada { Id = 5, Texto = "O que uma impressora disse para a outra? Essa folha é sua ou é impressão minha?", Categoria = "Tecnologia" },
                new Piada { Id = 6, Texto = "Por que a plantinha não foi ao médico? Porque só tinha médico de plantão.", Categoria = "Natureza" },
                new Piada { Id = 7, Texto = "O que o pato disse para a pata? Vem Quá!", Categoria = "Animais" },
                new Piada { Id = 8, Texto = "Qual o pé que é mais rápido? O pé-ligeiro.", Categoria = "Geral" },
                new Piada { Id = 9, Texto = "Por que o pinheiro não se perde na floresta? Porque ele tem uma pinha.", Categoria = "Natureza" },
                new Piada { Id = 10, Texto = "O que o tomate foi fazer no banco? Tirar extrato.", Categoria = "Comida" },
                new Piada { Id = 11, Texto = "Qual é a tecla preferida do astronauta? A barra de espaço.", Categoria = "Tecnologia" },
                new Piada { Id = 12, Texto = "Por que o jacaré tirou o filho da escola? Porque ele réptil de ano.", Categoria = "Animais" },
                new Piada { Id = 13, Texto = "Qual é o rei dos queijos? O Requeijão.", Categoria = "Comida" },
                new Piada { Id = 14, Texto = "O que é um ponto verde na antártida? Um ping-green.", Categoria = "Geral" },
                new Piada { Id = 15, Texto = "Por que o bombeiro não gosta de andar? Porque ele socorre.", Categoria = "Profissões" },
                new Piada { Id = 16, Texto = "Qual é o animal que não vale mais nada? O javali.", Categoria = "Animais" },
                new Piada { Id = 17, Texto = "O que o pagodeiro foi fazer na igreja? Cantar pá god.", Categoria = "Geral" },
                new Piada { Id = 18, Texto = "Por que a velhinha não usa relógio? Porque ela é sem hora.", Categoria = "Geral" },
                new Piada { Id = 19, Texto = "Como o Batman faz para entrar na Bat-caverna? Ele bat-palma.", Categoria = "Herois" },
                new Piada { Id = 20, Texto = "Qual o doce preferido do átomo? Pé-de-moleculas.", Categoria = "Ciencia" },
                new Piada { Id = 21, Texto = "O que a Lua disse ao Sol? Nossa, você é tão grande e não te deixam sair à noite!", Categoria = "Espaço" },
                new Piada { Id = 22, Texto = "Por que as estrelas não fazem miau? Porque Astronomia.", Categoria = "Ciencia" },
                new Piada { Id = 23, Texto = "O que a banana suicida falou? Macacos me mordam!", Categoria = "Comida" },
                new Piada { Id = 24, Texto = "Qual o estado que quer ser carro? Sergipe.", Categoria = "Geografia" },
                new Piada { Id = 25, Texto = "O que é, o que é: cai em pé e corre deitado? A chuva.", Categoria = "Charada" },
                new Piada { Id = 26, Texto = "Em qual cidade o Thor mora? Valhalla? Não, Pousada.", Categoria = "Geral" },
                new Piada { Id = 27, Texto = "Por que o elétron não foi à festa? Porque precisa ser positivo.", Categoria = "Ciencia" },
                new Piada { Id = 28, Texto = "O que o advogado do frango foi fazer? Foi soltar a franga.", Categoria = "Animais" },
                new Piada { Id = 29, Texto = "Qual a diferença entre o gato e a coca-cola? O gato faz miau e a coca-cola faz tshhh.", Categoria = "Animais" },
                new Piada { Id = 30, Texto = "O que o martelo foi fazer no culto? Pregador.", Categoria = "Ferramentas" }
             );
         });

         modelBuilder.Entity<Receita>(entity =>
         {
             entity.HasKey(r => r.Id);
             entity.Property(r => r.Nome).IsRequired();
             entity.Property(r => r.Ingredientes).IsRequired();
             
             entity.HasMany(r => r.Passos)
                   .WithOne(p => p.Receita)
                   .HasForeignKey(p => p.ReceitaId)
                   .OnDelete(DeleteBehavior.Cascade);

             // Seed 5 Recipes
             entity.HasData(
                 new Receita { Id = 1, Nome = "Bolo de Cenoura", Categoria = "Doces", Ingredientes = "3 cenouras, 4 ovos, 1 xícara de óleo, 2 xícaras de açúcar, 2 xícaras de farinha, 1 colher de fermento." },
                 new Receita { Id = 2, Nome = "Omelete Simples", Categoria = "Salgados", Ingredientes = "2 ovos, sal a gosto, queijo, presunto, orégano." },
                 new Receita { Id = 3, Nome = "Arroz Branco", Categoria = "Acompanhamentos", Ingredientes = "1 xícara de arroz, 2 xícaras de água, alho, sal, óleo." },
                 new Receita { Id = 4, Nome = "Brigadeiro", Categoria = "Doces", Ingredientes = "1 lata de leite condensado, 4 colheres de chocolate em pó, 1 colher de manteiga, granulado." },
                 new Receita { Id = 5, Nome = "Suco de Limão", Categoria = "Bebidas", Ingredientes = "3 limões, 1 litro de água, açúcar ou adoçante a gosto, gelo." }
             );
         });

         modelBuilder.Entity<ReceitaPasso>(entity =>
         {
             entity.HasKey(p => p.Id);
             entity.Property(p => p.Descricao).IsRequired();

             // Seed Steps (Deterministic IDs)
             entity.HasData(
                 // Bolo de Cenoura (Id 1)
                 new ReceitaPasso { Id = 1, ReceitaId = 1, Ordem = 1, Descricao = "Descasque e corte as cenouras em rodelas." },
                 new ReceitaPasso { Id = 2, ReceitaId = 1, Ordem = 2, Descricao = "No liquidificador, bata as cenouras, os ovos e o óleo." },
                 new ReceitaPasso { Id = 3, ReceitaId = 1, Ordem = 3, Descricao = "Em uma tigela, misture o açúcar, a farinha e o fermento." },
                 new ReceitaPasso { Id = 4, ReceitaId = 1, Ordem = 4, Descricao = "Despeje a mistura do liquidificador na tigela e mexa bem." },
                 new ReceitaPasso { Id = 5, ReceitaId = 1, Ordem = 5, Descricao = "Unte uma forma e despeje a massa." },
                 new ReceitaPasso { Id = 6, ReceitaId = 1, Ordem = 6, Descricao = "Asse em forno pré-aquecido a 180 graus por 40 minutos." },

                 // Omelete (Id 2)
                 new ReceitaPasso { Id = 7, ReceitaId = 2, Ordem = 1, Descricao = "Quebre os ovos em um prato fundo." },
                 new ReceitaPasso { Id = 8, ReceitaId = 2, Ordem = 2, Descricao = "Bata os ovos ligeiramente com um garfo." },
                 new ReceitaPasso { Id = 9, ReceitaId = 2, Ordem = 3, Descricao = "Tempere com sal e orégano." },
                 new ReceitaPasso { Id = 10, ReceitaId = 2, Ordem = 4, Descricao = "Aqueça uma frigideira com um pouco de óleo." },
                 new ReceitaPasso { Id = 11, ReceitaId = 2, Ordem = 5, Descricao = "Despeje os ovos e adicione o queijo e presunto." },
                 new ReceitaPasso { Id = 12, ReceitaId = 2, Ordem = 6, Descricao = "Dobre ao meio e deixe dourar dos dois lados." },

                 // Arroz (Id 3)
                 new ReceitaPasso { Id = 13, ReceitaId = 3, Ordem = 1, Descricao = "Lave o arroz se desejar." },
                 new ReceitaPasso { Id = 14, ReceitaId = 3, Ordem = 2, Descricao = "Aqueça o óleo e refogue o alho picado." },
                 new ReceitaPasso { Id = 15, ReceitaId = 3, Ordem = 3, Descricao = "Adicione o arroz e refogue por um minuto." },
                 new ReceitaPasso { Id = 16, ReceitaId = 3, Ordem = 4, Descricao = "Adicione a água fervente e o sal." },
                 new ReceitaPasso { Id = 17, ReceitaId = 3, Ordem = 5, Descricao = "Cozinhe em fogo baixo com a panela semi-tampada." },
                 new ReceitaPasso { Id = 18, ReceitaId = 3, Ordem = 6, Descricao = "Quando a água secar, desligue e deixe descansar." },

                 // Brigadeiro (Id 4)
                 new ReceitaPasso { Id = 19, ReceitaId = 4, Ordem = 1, Descricao = "Em uma panela, coloque o leite condensado." },
                 new ReceitaPasso { Id = 20, ReceitaId = 4, Ordem = 2, Descricao = "Adicione o chocolate em pó e a manteiga." },
                 new ReceitaPasso { Id = 21, ReceitaId = 4, Ordem = 3, Descricao = "Leve ao fogo baixo, mexendo sempre." },
                 new ReceitaPasso { Id = 22, ReceitaId = 4, Ordem = 4, Descricao = "Mexa até desgrudar do fundo da panela." },
                 new ReceitaPasso { Id = 23, ReceitaId = 4, Ordem = 5, Descricao = "Despeje em um prato untado e deixe esfriar." },
                 new ReceitaPasso { Id = 24, ReceitaId = 4, Ordem = 6, Descricao = "Enrole as bolinhas e passe no granulado." },
                 
                 // Suco de Limão (Id 5)
                 new ReceitaPasso { Id = 25, ReceitaId = 5, Ordem = 1, Descricao = "Lave bem os limões." },
                 new ReceitaPasso { Id = 26, ReceitaId = 5, Ordem = 2, Descricao = "Corte os limões ao meio." },
                 new ReceitaPasso { Id = 27, ReceitaId = 5, Ordem = 3, Descricao = "Esprema o suco dos limões em uma jarra." },
                 new ReceitaPasso { Id = 28, ReceitaId = 5, Ordem = 4, Descricao = "Adicione a água e misture." },
                 new ReceitaPasso { Id = 29, ReceitaId = 5, Ordem = 5, Descricao = "Adoce a gosto e mexa bem até dissolver." },
                 new ReceitaPasso { Id = 30, ReceitaId = 5, Ordem = 6, Descricao = "Adicione gelo e sirva imediatamente." }
             );
         });


        modelBuilder.Entity<UserFunState>(entity =>
         {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PiadasContadasIds).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ReceitasVistasIds).HasColumnType("nvarchar(max)");
            
            entity.HasOne(e => e.User)
                  .WithOne()
                  .HasForeignKey<UserFunState>(u => u.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
         });

         // --- Comodos Configuration ---

         modelBuilder.Entity<Comodo>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
             entity.Property(e => e.CriadoEm).HasColumnType("datetimeoffset").IsRequired();

             entity.HasOne(e => e.User)
                   .WithMany()
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
         });

         modelBuilder.Entity<ComodoDispositivo>(entity =>
         {
             entity.HasKey(e => new { e.ComodoId, e.DispositivoId });
             
             entity.Property(e => e.Papel).HasMaxLength(50);
             entity.Property(e => e.DispositivoId).HasMaxLength(100);
             entity.Property(e => e.Tipo).HasMaxLength(50);

             entity.HasOne(e => e.Comodo)
                   .WithMany(c => c.Dispositivos)
                   .HasForeignKey(e => e.ComodoId)
                   .OnDelete(DeleteBehavior.Cascade);
                   
             // No FK to Device
         });


         modelBuilder.Entity<EscopoConversacional>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.ExpiraEm).HasColumnType("datetimeoffset").IsRequired();
             entity.Property(e => e.CriadoEm).HasColumnType("datetimeoffset").IsRequired();

             entity.HasOne(e => e.User)
                   .WithMany()
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.NoAction); // Avoid circles, or Cascade if User deleted. Let's say NoAction or Cascade. Start with NoAction to be safe on multiple cascades.

             entity.HasOne(e => e.Comodo)
                   .WithMany()
                   .HasForeignKey(e => e.ComodoId)
                   .OnDelete(DeleteBehavior.Cascade);
         });

         // --- Automation / Routines Configuration ---

         modelBuilder.Entity<Rotina>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.Nome).IsRequired().HasMaxLength(150);
             entity.Property(e => e.Descricao).HasMaxLength(300);
             entity.Property(e => e.CriadaEm).HasColumnType("datetimeoffset").IsRequired();
             entity.Property(e => e.AtualizadaEm).HasColumnType("datetimeoffset").IsRequired();

             entity.HasOne(e => e.User)
                   .WithMany()
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
             
             entity.HasMany(e => e.Gatilhos)
                   .WithOne(g => g.Rotina)
                   .HasForeignKey(g => g.RotinaId)
                   .OnDelete(DeleteBehavior.Cascade);

             entity.HasMany(e => e.Acoes)
                   .WithOne(a => a.Rotina)
                   .HasForeignKey(a => a.RotinaId)
                   .OnDelete(DeleteBehavior.Cascade);
         });

         modelBuilder.Entity<RotinaGatilho>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.Expressao).IsRequired().HasMaxLength(300);
             entity.Property(e => e.DiasSemana).HasMaxLength(50);
             entity.Property(e => e.Tipo).IsRequired();
         });

         modelBuilder.Entity<RotinaAcao>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.Payload).IsRequired();
             entity.Property(e => e.OrdemExecucao).IsRequired();
             entity.Property(e => e.Tipo).IsRequired();
         });

         modelBuilder.Entity<Lembrete>(entity =>
         {
             entity.HasKey(e => e.Id);
             entity.Property(e => e.Texto).IsRequired();
             entity.Property(e => e.DispararEm).HasColumnType("datetimeoffset").IsRequired();
             entity.Property(e => e.DataCriacao).HasColumnType("datetimeoffset").IsRequired();
             entity.Property(e => e.Status).IsRequired().HasConversion<string>();

             entity.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de SupportLearning
        modelBuilder.Entity<SupportLearning>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserEntradaTxt).IsRequired();
            entity.Property(e => e.IAResponseTxt).IsRequired();
            entity.Property(e => e.ContextTitle).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ConfidenceScore).IsRequired();
            entity.Property(e => e.UsageCount).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });


     }
}
