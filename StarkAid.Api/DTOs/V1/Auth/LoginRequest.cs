namespace StarkAid.Api.DTOs.V1.Auth
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Origem do login: "app" ou "web"
        public string Origem { get; set; }
    }
}
