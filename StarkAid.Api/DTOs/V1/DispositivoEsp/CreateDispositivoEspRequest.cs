using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.DTOs.V1.DispositivoEsp;

public class CreateDispositivoEspRequest
{
    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string Ip { get; set; } = string.Empty;

    [Required]
    [Range(1, 65535)]
    public int Porta { get; set; }

    [MaxLength(200)]
    public string? Comando { get; set; }

    [MaxLength(200)]
    public string? ComandToEsp { get; set; }
}

