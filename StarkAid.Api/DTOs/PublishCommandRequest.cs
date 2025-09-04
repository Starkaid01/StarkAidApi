using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.DTOs
{
    public enum CommandType
    {
        ligar,
        desligar,
        reiniciar,
        resetar
    }

    public class PublishCommandRequest
    {
        [Required]
        public Guid DeviceId { get; set; }

        public CommandType? Command { get; set; }

        public string? CustomCommand { get; set; } // Tornado nullable
    }
}