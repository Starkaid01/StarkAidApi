namespace StarkAid.Api.Entities
{
    public class FirebaseToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime DataCadastro { get; set; }
    }
}
