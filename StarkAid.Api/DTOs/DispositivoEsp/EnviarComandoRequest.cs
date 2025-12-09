using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.DTOs.DispositivoEsp;

public class EnviarComandoRequest
{
    [Required]
    public string Comando { get; set; } = string.Empty;
}

