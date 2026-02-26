namespace Accountancy.Shared.Response;

public sealed record AccountancyDetailResponse(
    Guid Id,
    string CNPJ,
    string LegalName,
    string? TradeName,
    string Address,
    string City,
    string State,
    string PostalCode,
    string Phone,
    string? Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);