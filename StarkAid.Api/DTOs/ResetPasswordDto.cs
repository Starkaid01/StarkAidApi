namespace StarkAid.Api.DTOs
{
    public class ResetPasswordDto
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string RepeatNewPassword { get; set; } = null!;
    }
}
