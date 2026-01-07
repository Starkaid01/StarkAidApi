using System;

namespace StarkAid.Api.DTOs.Commands
{
    public sealed class CommandRequestDto
    {
        public Guid UserId { get; set; }
        public string Origem { get; set; } = string.Empty; // Android, Web, WhatsApp
        public string Texto { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public string Contexto { get; set; } = "privado"; // privado, global, followup
        public int ExecutionDepth { get; set; } = 0;
        public bool UseStarkCoins { get; set; } = false;
    }
}
