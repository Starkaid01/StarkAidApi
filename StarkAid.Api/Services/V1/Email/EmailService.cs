using MailKit.Net.Smtp;
using MimeKit;
using StarkAid.Api.Services.V1.Email;

namespace StarkAid.Api.Services.V1.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration) => _configuration = configuration;

    public async Task SendAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("StarkAid", _configuration["EmailSettings:From"]));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"],
                                  int.Parse(_configuration["EmailSettings:Port"]), false);
        await client.AuthenticateAsync(_configuration["EmailSettings:Username"],
                                      _configuration["EmailSettings:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
