using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class ErrorCodeDescription
{
    [Key]
    [MaxLength(50)]
    public string CodigoDeErro { get; set; } = string.Empty;
    
    [Required]
    public string Descricao { get; set; } = string.Empty;
    
    [Required]
    public string Contexto { get; set; } = string.Empty;
    
    [Required]
    public string CamposRelevantes { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Origem { get; set; } = "soft"; // "soft" ou "app"
    
    public string? Solucoes { get; set; } // JSON array de soluções
}

