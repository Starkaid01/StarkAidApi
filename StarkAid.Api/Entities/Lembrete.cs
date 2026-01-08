using System;

namespace StarkAid.Api.Entities
{
    public class Lembrete
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public string Texto { get; set; }

        public DateTimeOffset DispararEm { get; set; }

        public bool PushEnviado { get; set; }
        public bool Falado { get; set; }

        public LembreteStatus Status { get; set; } = LembreteStatus.Pendente;

        public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;
    }

    public enum LembreteStatus
    {
        Pendente,
        Disparado,
        Concluido
    }
}
