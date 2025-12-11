namespace StarkAid.Api.Services.V1.Email;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}
