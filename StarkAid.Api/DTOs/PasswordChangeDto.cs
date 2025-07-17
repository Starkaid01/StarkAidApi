namespace StarkAid.Api.DTOs;

public class PasswordChangeDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
