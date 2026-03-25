using Core.Application.ServiceLifetimes;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Core.Infrastructure.Services;

public class EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger) : IEmailService, IScoped
{
    private readonly EmailOptions _settings = options.Value;

    public async Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            logger.LogError(
                "EmailSettings:SmtpHost is not configured. To={ToEmail}, Subject={Subject}",
                toEmail, subject);

            throw new InvalidOperationException("EmailOptions:SmtpHost is not configured.");
        }

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(new MailAddress(toEmail, toName));

        logger.LogInformation(
            "Sending email. To={ToEmail}, Subject={Subject}",
            toEmail, subject);

        await client.SendMailAsync(message, cancellationToken);

        logger.LogInformation(
            "Email sent successfully. To={ToEmail}, Subject={Subject}",
            toEmail, subject);
    }
}