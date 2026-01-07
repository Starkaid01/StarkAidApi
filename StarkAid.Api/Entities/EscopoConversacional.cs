namespace StarkAid.Api.Entities
{
    public class EscopoConversacional
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid ComodoId { get; set; }
        public DateTimeOffset ExpiraEm { get; set; }
        public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

        // Navigation Properties
        public User? User { get; set; }
        public Comodo? Comodo { get; set; }
    }
}
