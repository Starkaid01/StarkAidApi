using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities;

public class Device
{
    [Key] public Guid Id { get; set; }

    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)] public string ApiKey { get; set; } = string.Empty;

    [Required] public Guid UserId { get; set; }

    [Required, MaxLength(200)] public string MqttTopic { get; set; } = string.Empty;

    [MaxLength(200)] public string? Comando { get; set; }

    public User User { get; set; } = null!;
}
