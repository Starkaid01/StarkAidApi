using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities
{
    public class RotinaGatilho
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RotinaId { get; set; }

        [Required]
        public TipoGatilho Tipo { get; set; }

        [Required]
        [MaxLength(300)]
        public string Expressao { get; set; } = string.Empty; // ex: "08:00", "boa noite", "sensor.status.on"

        [MaxLength(50)]
        public string? DiasSemana { get; set; } // ex: "1,2,3,4,5" (1=seg, 7=dom)

        [ForeignKey(nameof(RotinaId))]
        public virtual Rotina Rotina { get; set; } = null!;
    }
}
