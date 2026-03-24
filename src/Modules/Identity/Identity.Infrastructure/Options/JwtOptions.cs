using System.ComponentModel.DataAnnotations;

namespace Identity.Infrastructure.Options;

public sealed record JwtOptions
{
    [Required]
    public required string Key { get; init; }

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }
}