namespace StarkAid.WindowsForms.Models;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int StarkCoinBalance { get; set; }
    public string PlanType { get; set; } = "Free";
    public int TokensConsumidosSemana { get; set; }
    public int TokensSemanaMax { get; set; }
    public int TokensRestantes { get; set; }
    public bool AdsEnabled { get; set; }
    public int AgendamentosMax { get; set; }
    public int AgendamentosRestantes { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Estado { get; set; }
    public string? Cidade { get; set; }
    public string? Bairro { get; set; }
}

