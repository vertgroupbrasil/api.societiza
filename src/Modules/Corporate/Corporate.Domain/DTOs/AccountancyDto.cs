using Core.Domain.Enumerations;

namespace Corporate.Domain.DTOs;

public static class AccountancyDto
{
    public sealed record UpdateAccountancy
    {
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
        public DateTimeOffset ExpirationDate { get; set; }
        public DatabaseProvider? DatabaseProvider { get; set; }
        public string? DatabaseConnectionString { get; set; }
    }
}