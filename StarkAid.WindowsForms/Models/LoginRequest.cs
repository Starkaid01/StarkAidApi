namespace StarkAid.WindowsForms.Models;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Origem { get; set; } = "app";
}

