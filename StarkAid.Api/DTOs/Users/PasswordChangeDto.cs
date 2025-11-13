namespace StarkAid.Api.DTOs.Users;

public class PasswordChangeDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
