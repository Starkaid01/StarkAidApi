using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarkAid.Api.Entities
{
    public class UserFunState
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        
        // JSON array of int (IDs)
        public string PiadasContadasIds { get; set; } = "[]"; 

        public int? ReceitaAtualId { get; set; }
        public int PassoAtual { get; set; } = 0;
        public bool IniciouPassoAPasso { get; set; } = false;

        // JSON array of int (IDs)
        public string ReceitasVistasIds { get; set; } = "[]";

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
