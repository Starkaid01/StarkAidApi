namespace StarkAid.Api.DTOs.V1.Admin;

public class UserDashboardResponse
{
    public UserDto User { get; set; } = null!;
    
    // Contadores
    public int QuantidadeDispositivosEsp { get; set; }
    public int QuantidadeDispositivosEwelink { get; set; }
    public int QuantidadeDispositivosStarkSwitch { get; set; }
    public int TotalComandosSociais { get; set; }
    
    // Últimos comandos (Combined mainly for backward compat or simple view)
    public string? UltimoComandoEsp { get; set; }
    public string? UltimoComandoEwelink { get; set; }
    public string? UltimoComandoStarkSwitch { get; set; }
    public string? UltimoComandoSocial { get; set; }
    public string? UltimaRespostaSocial { get; set; }
    public string? UltimoComandoIA { get; set; }
    public string? UltimaRespostaIA { get; set; }
    
    // Novos campos
    public bool UsuarioOnline { get; set; }
    public string? UltimoFormAcessado { get; set; }
    public DateTimeOffset? UltimaActivityAcessada { get; set; }

    // Separated Activities
    public UserActivityDto? ActivitySoft { get; set; }
    public UserActivityDto? ActivityApp { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int StarkCoinBalance { get; set; }
    public string? ApiKey { get; set; }
    public string? Estado { get; set; }
    public string? Cidade { get; set; }
    public string? Bairro { get; set; }
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
    public string? UltimoDispositivoAcionado { get; set; } // Added based on requirements
    public string? UltimaUiAcessada { get; set; }
}
