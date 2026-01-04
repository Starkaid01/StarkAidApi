using System;
using System.ComponentModel.DataAnnotations;

namespace StarkAid.Api.Entities
{
    public class AprendizadoResposta
    {
        [Key]
        public Guid Id { get; set; }

        public Guid AprendizadoId { get; set; }

        [Required]
        public string Texto { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade de vezes que esta variação foi escolhida.
        /// </summary>
        public int UsoCount { get; set; } = 0;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property
        public Aprendizado Aprendizado { get; set; } = null!;
    }
}
