namespace StarkAid.Web.Dtos
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;  // Adicionado campo obrigatório
    }
}