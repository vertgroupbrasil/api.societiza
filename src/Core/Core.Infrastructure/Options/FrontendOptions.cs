using System.ComponentModel.DataAnnotations;

namespace Core.Infrastructure.Options;

public sealed record FrontendOptions
{
    [Required]
    public string BaseUrl { get; init; } = string.Empty;
}