using System.ComponentModel.DataAnnotations;

namespace Core.Infrastructure.Options;

public sealed record EmailOptions
{
    [Required]
    public string SmtpHost { get; init; } = string.Empty;

    [Required, Range(1, 65535)]
    public int SmtpPort { get; init; } = 587;

    [Required]
    public bool EnableSsl { get; init; } = true;

    [Required]
    public string FromName { get; init; } = "Societiza";

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required, EmailAddress]
    public string FromEmail { get; init; } = string.Empty;
}