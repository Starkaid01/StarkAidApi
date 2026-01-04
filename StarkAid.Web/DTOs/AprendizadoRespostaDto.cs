using System;

namespace StarkAid.Web.DTOs
{
    public class AprendizadoRespostaDto
    {
        public Guid Id { get; set; }
        public Guid AprendizadoId { get; set; }
        public string Texto { get; set; } = string.Empty;
        public int UsoCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
