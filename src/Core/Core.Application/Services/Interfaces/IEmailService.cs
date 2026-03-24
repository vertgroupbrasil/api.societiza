namespace Core.Application.Services.Interfaces;

public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string body, CancellationToken cancellationToken = default);
}
