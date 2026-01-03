using Core.Application.Messaging.Commands;
using Core.Domain.Enumerations;
using Corporate.Shared.Response.Accountancy;

namespace Corporate.Shared.Commands.Accountancy;

public sealed record CreateAccountancyCommand(
    string LegalName,
    string CNPJ,
    string Address,
    string City,
    string State,
    string PostalCode,
    string Phone,
    string Email,

    string? TradeName = null,
    string? Notes = null,

    DatabaseProvider? DatabaseProvider = null,
    string? DatabaseConnectionString = null,

    DateTimeOffset? ExpirationDate = null
) : ICommand<CreateAccountancyResponse>;