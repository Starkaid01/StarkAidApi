using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.DTOs.V1.DispositivoEsp;

public class EnviarComandoRequest
{
    [Required]
    public string Comando { get; set; } = string.Empty;
}

