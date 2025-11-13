namespace StarkAid.Api.Services.Users
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }
}
