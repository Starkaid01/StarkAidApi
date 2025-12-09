using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.DTOs.DispositivoEsp;

public class UpdateDispositivoEspRequest
{
    [MaxLength(150)]
    public string? Nome { get; set; }

    [MaxLength(45)]
    public string? Ip { get; set; }

    [Range(1, 65535)]
    public int? Porta { get; set; }

    [MaxLength(200)]
    public string? Comando { get; set; }

    [MaxLength(200)]
    public string? ComandToEsp { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    public bool? LigadoDesligado { get; set; }
}

