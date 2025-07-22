using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using UnityHub.Application.Interfaces.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpHost = _configuration["Smtp:Host"];
        var smtpPort = int.Parse(_configuration["Smtp:Port"]);
        var smtpUser = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromEmail = _configuration["Smtp:From"];

        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@"))
            throw new FormatException($"Invalid recipient email: {toEmail}");

        if (string.IsNullOrWhiteSpace(fromEmail) || !fromEmail.Contains("@"))
            throw new FormatException($"Invalid sender email: {fromEmail}");

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, password),
            EnableSsl = true
        };

        var from = new MailAddress(fromEmail, "UnityHub Admin");
        var to = new MailAddress(toEmail);

        var message = new MailMessage(from, to)
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        await client.SendMailAsync(message);
    }
}
