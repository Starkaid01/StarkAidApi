namespace StarkAid.Api.DTOs.Users
{
    public class ResetPasswordDto
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string RepeatNewPassword { get; set; } = null!;
    }
}
