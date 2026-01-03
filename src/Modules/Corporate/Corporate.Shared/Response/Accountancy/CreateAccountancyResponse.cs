namespace Corporate.Shared.Response.Accountancy;

public sealed record CreateAccountancyResponse
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
}