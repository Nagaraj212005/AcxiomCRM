using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace AcxiomCRM.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = _configuration["Email:SmtpHost"];
        var senderAddress = _configuration["Email:SenderAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(senderAddress))
        {
            throw new InvalidOperationException(
                "Email delivery is not configured. Set Email:SmtpHost and Email:SenderAddress.");
        }

        var port = _configuration.GetValue<int?>("Email:SmtpPort") ?? 587;
        using var message = new MailMessage(senderAddress, email, subject, htmlMessage)
        {
            IsBodyHtml = true
        };
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = _configuration.GetValue("Email:EnableSsl", true)
        };

        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(message);
    }
}
