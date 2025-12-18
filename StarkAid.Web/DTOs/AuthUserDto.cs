namespace StarkAid.Web.Dtos
{
    public class AuthUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int StarkCoinBalance { get; set; }  // Corrigido de StarkCoins para StarkCoinBalance
        public string PlanType { get; set; } = string.Empty;
    }
}