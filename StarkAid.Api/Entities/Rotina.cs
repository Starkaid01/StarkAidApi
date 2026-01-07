using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities
{
    public class Rotina
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Descricao { get; set; }

        public bool Ativa { get; set; } = true;

        public DateTimeOffset CriadaEm { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset AtualizadaEm { get; set; } = DateTimeOffset.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<RotinaGatilho> Gatilhos { get; set; } = new List<RotinaGatilho>();
        public virtual ICollection<RotinaAcao> Acoes { get; set; } = new List<RotinaAcao>();
    }
}
