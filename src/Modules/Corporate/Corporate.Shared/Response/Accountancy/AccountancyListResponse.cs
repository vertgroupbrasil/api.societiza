namespace Corporate.Shared.Response.Accountancy;

public sealed record AccountancyListResponse
{
    public Guid Id { get; init; }
    public string LegalName { get; init; } = string.Empty;
    public string? TradeName { get; init; }
    public string CNPJ { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string TenantName { get; init; } = string.Empty;
    public int ActiveModulesCount { get; init; }
    public bool IsDeleted { get; init; }
}