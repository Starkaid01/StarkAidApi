namespace StarkAid.Web.Dtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

        public AuthUserDto User { get; set; } = null!;
    }
}
