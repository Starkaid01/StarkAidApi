using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ApiKey { get; set; } = string.Empty;

        [Required]
        [Range(0, 9999999999999999.99)]
        public decimal StarkCoins { get; set; } // decimal(18,2) no DbContext

        [Required]
        public DateTimeOffset CreatedAt { get; set; } // datetimeoffset no DbContext

        [Required]
        public bool IsActive { get; set; }

        [Required, MaxLength(50)]
        public string Role { get; set; } = "UserNivel1";

        [MaxLength(100)]
        public string? PreapprovalId { get; set; }

        public DateTimeOffset? UltimoPagamentoConfirmadoEm { get; set; } // datetimeoffset no DbContext


        // Spotify Integration
        [MaxLength(500)]
        public string? SpotifyAccessToken { get; set; }

        [MaxLength(500)]
        public string? SpotifyRefreshToken { get; set; }

        public DateTimeOffset? SpotifyTokenExpiresAt { get; set; }

        // Navegação
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
        public ICollection<FirebaseToken> FirebaseTokens { get; set; } = new List<FirebaseToken>();
        public ICollection<Device> Devices { get; set; } = new List<Device>();
        public ICollection<DispositivoDisparo> DispositivosDisparo { get; set; } = new List<DispositivoDisparo>();
        public ICollection<Disparo> Disparos { get; set; } = new List<Disparo>();
        public ICollection<ComandoSocial> ComandosSociais { get; set; } = new List<ComandoSocial>();
        public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();

        public virtual ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();
    }
}