using System;
using StarkAid.Api.DTOs.Telemetry;

namespace StarkAid.Api.Entities
{
    public class Telemetria
    {
        public Telemetria() { }

        public Telemetria(TelemetryEventDto dto)
        {
            UserId = dto.UserId;
            Origem = dto.Origem;
            Evento = dto.Evento;
            Categoria = dto.Categoria;
            MetadataJson = dto.MetadataJson;
            CriadoEm = dto.CriadoEm;
        }

        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Origem { get; set; } = string.Empty;
        public string Evento { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string? MetadataJson { get; set; }
        public DateTimeOffset CriadoEm { get; set; }

        public User? User { get; set; }
    }
}
