using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities;

public enum PlanoStarkAid
{
    Free = 1,
    Premium = 2
}

public enum UserPlanType
{
    Free = 0,
    Premium = 1
}

public class User
{
    [Key] public Guid Id { get; set; }

    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;

    [Required, MaxLength(256)] public string Email { get; set; } = string.Empty;

    [Required] public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)] public string ApiKey { get; set; } = string.Empty;

    [Required] public int StarkCoins { get; set; } = 0;


    [Required] public UserPlanType PlanType { get; set; } = UserPlanType.Free;

    [NotMapped]
    public PlanoStarkAid Plano
    {
        get => PlanType == UserPlanType.Premium ? PlanoStarkAid.Premium : PlanoStarkAid.Free;
        set => PlanType = value == PlanoStarkAid.Premium ? UserPlanType.Premium : UserPlanType.Free;
    }

    [Required] public int TokensConsumidosSemana { get; set; }

    [Required] public DateTimeOffset CreatedAt { get; set; }

    [Required] public bool IsActive { get; set; }

    [Required, MaxLength(50)] public string Role { get; set; } = "UserNivel1";

    [Required, MaxLength(50)] public string RemovalAds { get; set; } = "Desativado";

    [MaxLength(100)] public string? PreapprovalId { get; set; }

    [MaxLength(100)] public string? Estado { get; set; }

    [MaxLength(100)] public string? Cidade { get; set; }

    [MaxLength(100)] public string? Bairro { get; set; }

    public DateTimeOffset? LastUpdatedAt { get; set; }

    public DateTimeOffset? UltimoPagamentoConfirmadoEm { get; set; }

    // Spotify integration
    [MaxLength(500)] public string? SpotifyAccessToken { get; set; }
    [MaxLength(500)] public string? SpotifyRefreshToken { get; set; }
    public DateTimeOffset? SpotifyTokenExpiresAt { get; set; }

    // Estatística de reconhecimento de voz
    public double MinutosReconhecidos { get; set; }

    public string? WhatsAppSessionData { get; set; }

    // Navegações
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<FirebaseToken> FirebaseTokens { get; set; } = new List<FirebaseToken>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<DispositivoDisparo> DispositivosDisparo { get; set; } = new List<DispositivoDisparo>();
    public ICollection<Disparo> Disparos { get; set; } = new List<Disparo>();
    public ICollection<ComandoSocial> ComandosSociais { get; set; } = new List<ComandoSocial>();
    public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    public ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();
}
