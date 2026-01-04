namespace StarkAid.Web.DTOs
{
    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public string ApiStatus { get; set; }
        public string MqttStatus { get; set; }
        public bool MqttConnected { get; set; }
    }

    public class AdminUserListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }
        public int StarkCoins { get; set; }
        public string RemovalAds { get; set; }
    }

    public class UserDashboardDto
    {
        public AdminUserListDto User { get; set; }
        public int QuantidadeDispositivosEsp { get; set; }
        public int QuantidadeDispositivosEwelink { get; set; }
        public int QuantidadeDispositivosStarkSwitch { get; set; }
        public int TotalComandosSociais { get; set; }

        public string UltimoComandoEsp { get; set; }
        public string UltimoComandoEwelink { get; set; }
        public string UltimoComandoStarkSwitch { get; set; }

        public string UltimoComandoSocial { get; set; }
        public string UltimaRespostaSocial { get; set; }

        public string UltimoComandoIA { get; set; }
        public string UltimaRespostaIA { get; set; }

        public bool UsuarioOnline { get; set; }
        public string UltimoFormAcessado { get; set; }
        public DateTimeOffset? UltimaActivityAcessada { get; set; }

        public UserActivityDto? ActivitySoft { get; set; }
        public UserActivityDto? ActivityApp { get; set; }
    }

    public class UserActivityDto
    {
        public string? UltimoComandoEsp { get; set; }
        public string? UltimoComandoEwelink { get; set; }
        public string? UltimoComandoStarkSwitch { get; set; }
        public string? UltimoComandoSocial { get; set; }
        public string? UltimaRespostaSocial { get; set; }
        public string? UltimoComandoIA { get; set; }
        public string? UltimaRespostaIA { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }
        public string? UltimoDispositivoAcionado { get; set; }
        public string? UltimaUiAcessada { get; set; }
    }

    public class UserWithPlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public int StarkCoinBalance { get; set; }
        public string Plano { get; set; }
        public decimal Valor { get; set; }
        public string Status { get; set; }
        public DateTimeOffset? ExpiraEm { get; set; }
        public Guid AssinaturaId { get; set; }
        public DateTimeOffset DataCriacao { get; set; }
    }

    public class IniciarManutencaoRequest
    {
        public Guid UserId { get; set; }
        public string? NomeAssistente { get; set; }
    }
    
    public class UltimosComandosResponse
    {
        public string? UltimoComandoIA { get; set; }
        public string? UltimaRespostaIA { get; set; }
        public string? UltimoComandoAutomacao { get; set; }
        public string? UltimoComandoSocial { get; set; }
        public string? UltimaRespostaSocial { get; set; }
    }

    public class AdminUpdateDeviceRequest
    {
        public string? Name { get; set; }
        public string? Comando { get; set; }
    }

    public class AdminUpdateComandoSocialRequest
    {
        public string? Comando { get; set; }
        public string? Resposta { get; set; }
        public string? RespostasAleatorias { get; set; }
    }
}
