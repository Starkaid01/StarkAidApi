using Newtonsoft.Json;

namespace StarkAid.WindowsForms.Models;

public class ComandoDispositivoDto
{
    [JsonProperty("nome")]
    public string Nome { get; set; } = string.Empty;
    
    [JsonProperty("ip")]
    public string Ip { get; set; } = string.Empty;
    
    [JsonProperty("porta")]
    public int Porta { get; set; }
    
    [JsonProperty("comando")]
    public string Comando { get; set; } = string.Empty;
    
    [JsonProperty("comandToEsp")]
    public string? ComandToEsp { get; set; }
}

