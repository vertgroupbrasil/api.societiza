using Corporate.Domain.DTOs;

namespace Corporate.Shared.Response.Accountancy;

public sealed record AccountancyDetailResponse
{
    public Guid Id { get; init; }
    public string LegalName { get; init; } = string.Empty;
    public string? TradeName { get; init; }
    public string CNPJ { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public TenantDto.TenantSummary Tenant { get; init; } = null!;
}