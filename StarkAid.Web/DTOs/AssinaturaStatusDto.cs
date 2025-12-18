namespace StarkAid.Web.DTOs
{
    public class AssinaturaStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public DateTime IniciadaEm { get; set; }
        public DateTime? CanceladaEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public DateTime? PagamentoConfirmadoEm { get; set; }
        public string StripeCustomerId { get; set; } = string.Empty;
        public string StripeSubscriptionId { get; set; } = string.Empty;
        public string StripePriceId { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }
}
