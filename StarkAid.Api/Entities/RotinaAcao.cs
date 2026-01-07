using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities
{
    public class RotinaAcao
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RotinaId { get; set; }

        [Required]
        public int OrdemExecucao { get; set; }

        [Required]
        public TipoAcao Tipo { get; set; }

        [Required]
        public string Payload { get; set; } = string.Empty; // JSON com dados da ação

        [ForeignKey(nameof(RotinaId))]
        public virtual Rotina Rotina { get; set; } = null!;
    }
}
