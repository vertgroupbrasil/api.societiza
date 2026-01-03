using Core.Domain.Enumerations;

namespace Web.Endpoints.Corporate.Accountancy.Requests;

public class CreateAccountancyRequest
{
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string CNPJ { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public DatabaseProvider? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }
}