namespace StarkAid.Api.Entities
{
    public class PasswordResetToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime Expiration { get; set; }
    }
}
