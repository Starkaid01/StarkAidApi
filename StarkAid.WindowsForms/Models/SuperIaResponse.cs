namespace StarkAid.WindowsForms.Models;

public class SuperIaResponse
{
    public string Texto { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public string Modelo { get; set; } = string.Empty;
}

