namespace StarkAid.WindowsForms.Models;

public class SuperIaResponse
{
    // Resultado aninhado
    public IaResult? Resultado { get; set; }
    
    // Campos diretos (compatibilidade)
    public string Texto { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public string Modelo { get; set; } = string.Empty;
    
    // Economic payload
    public EconomicPayload? Economy { get; set; }
    
    // Campos diretos de economy (compatibilidade)
    public string? PlanType { get; set; }
    public int? TokensRestantes { get; set; }
    public int? TokensConsumidosSemana { get; set; }
    public int? TokensSemanaMax { get; set; }
    public int? StarkCoinBalance { get; set; }
    public bool? AdsEnabled { get; set; }
    public int? AgendamentosMax { get; set; }
    public int? Rate { get; set; }
    
    // Helper para obter texto (prioriza resultado aninhado)
    public string GetTexto()
    {
        return Resultado?.Texto ?? Texto;
    }
}

public class IaResult
{
    public string Texto { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public string? Modelo { get; set; }
}

