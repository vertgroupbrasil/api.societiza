using System.ComponentModel.DataAnnotations;

namespace Payment.Shared.Options;

public sealed class AbacatePayOptions
{
    public const string SectionName = "AbacatePay";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableIdempotency { get; set; } = true;
}