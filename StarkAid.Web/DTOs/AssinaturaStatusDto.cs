namespace StarkAid.Web.DTOs
{
    public class AssinaturaStatusDto
    {
        public string? Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int Nivel { get; set; }
        public string? NomePlano { get; set; }
        public DateTime IniciadaEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public DateTime? DataCriacao { get; set; }
        public string StripeSubscriptionId { get; set; } = string.Empty;
        
        // Mantendo compatibilidade com campos antigos se necessário
        public DateTime? CanceladaEm { get; set; }
        public DateTime? PagamentoConfirmadoEm { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripePriceId { get; set; }
    }
}
