using System;

namespace StarkAid.Api.Entities;

public class WebhookLog
{
    public int Id { get; set; }
    public DateTime DataRecebida { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string DataId { get; set; } = string.Empty;
    public string JsonDetalhado { get; set; } = string.Empty;
}
