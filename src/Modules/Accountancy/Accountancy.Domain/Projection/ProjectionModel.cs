using Core.Domain.Primitives.Interfaces;

namespace Accountancy.Domain.Projection;

public static class ProjectionModel
{
    public record Accountancy(
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
        DateTimeOffset? UpdatedAt,
        bool IsDeleted = false
    ) : IProjectionModel { };
}