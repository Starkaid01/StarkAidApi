using System;

namespace StarkAid.Api.Entities;

public class ConfiguracaoSistema
{
    public int Id { get; set; }
    public string DominioCloudflare { get; set; } = string.Empty;
    public string DominioNlp { get; set; } = string.Empty;
    public DateTime UltimaAtualizacao { get; set; } = DateTime.UtcNow;
}
