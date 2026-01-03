namespace Corporate.Domain.DTOs;

public static class TenantDto
{
    public sealed record TenantSummary
    {
        public string Id { get; init; } = string.Empty;
        public string Identifier { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public bool HasDedicatedDatabase { get; init; }
        public DateTimeOffset EstablishedDate { get; init; }
        public DateTimeOffset ExpirationDate { get; init; }
        public bool IsExpired => ExpirationDate < DateTimeOffset.UtcNow;
        public int DaysUntilExpiration => (int)(ExpirationDate - DateTimeOffset.UtcNow).TotalDays;
        public int DaysActive => (int)(DateTimeOffset.UtcNow - EstablishedDate).TotalDays;
    };
}