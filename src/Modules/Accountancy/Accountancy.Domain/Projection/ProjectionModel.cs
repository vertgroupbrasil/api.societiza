using Core.Domain.Primitives.Interfaces;

namespace Accountancy.Domain.Projection;

public static class ProjectionModel
{
    public record Accountancy(
        Guid Id,
        string LegalName,
        string CNPJ,
        string? TradeName,
        string Address,
        string City,
        string State,
        string PostalCode,
        string Phone,
        string? Email,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt
    ) : IProjectionModel { };
}