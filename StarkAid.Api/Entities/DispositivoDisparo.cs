using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class DispositivoDisparo
{
    [Key] public Guid Id { get; set; }

    [Required] public Guid UserId { get; set; }

    [Required, MaxLength(150)] public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(200)] public string MqttTopic { get; set; } = string.Empty;

    [Required, MaxLength(200)] public string StatusTopic { get; set; } = string.Empty;

    [Required] public DateTimeOffset DataCadastro { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Disparo> Disparos { get; set; } = new List<Disparo>();
}
