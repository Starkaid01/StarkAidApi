using System;

namespace StarkAid.Api.Entities;

public class PagamentoAvulso
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal Valor { get; set; }
    public string Status { get; set; } = "pendente";
    public string StripeSessionId { get; set; } = null!;
    public string StripeCustomerId { get; set; } = null!;
    public DateTimeOffset DataCriacao { get; set; }
    public DateTimeOffset? PagamentoConfirmadoEm { get; set; }
}
