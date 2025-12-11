namespace StarkAid.Api.DTOs.V1.Users;

public class PasswordChangeDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
