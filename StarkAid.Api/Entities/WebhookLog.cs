namespace StarkAid.Api.Entities
{
    public class WebhookLog
    {
        public int Id { get; set; }
        public DateTime DataRecebida { get; set; }
        public string Tipo { get; set; } = "";
        public string Acao { get; set; } = "";
        public string DataId { get; set; } = "";
        public string JsonDetalhado { get; set; } = "";
    }
}
