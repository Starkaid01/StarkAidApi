using System;

namespace StarkAid.Api.DTOs.Telemetry
{
    public sealed class TelemetryEventDto
    {
        public Guid UserId { get; set; }
        public string Origem { get; set; } = string.Empty; // Android / API
        public string Evento { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty; // comando, erro, latencia
        public string? MetadataJson { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
