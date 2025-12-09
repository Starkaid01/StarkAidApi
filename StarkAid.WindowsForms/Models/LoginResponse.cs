namespace StarkAid.WindowsForms.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public User User { get; set; } = new();
}

