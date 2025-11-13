using FirebaseAdmin.Auth;

namespace StarkAid.Api.Options
{
    public class WppConnectOptions
    {
        public string TokenDeAutenticacao { get; set; } = string.Empty;
        public string NovoDominio { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string? UserId { get; set; } // usado apenas na atualização do domínio
    }
}
