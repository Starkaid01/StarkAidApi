namespace StarkAid.WindowsForms.Models;

public class SuperIaRequest
{
    public string Texto { get; set; } = string.Empty;
    public string ContextoUser { get; set; } = string.Empty;
    public string ContextoIA { get; set; } = string.Empty;
    public string Estilo { get; set; } = string.Empty;
    public bool UseStarkCoins { get; set; } = false;
}

