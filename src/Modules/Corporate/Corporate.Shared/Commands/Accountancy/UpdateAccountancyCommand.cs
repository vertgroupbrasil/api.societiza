using Core.Application.Messaging.Commands;
using Core.Domain.Enumerations;

namespace Corporate.Shared.Commands.Accountancy;

public sealed record UpdateAccountancyCommand(
    Guid Id,
    string LegalName,
    string CNPJ,
    string Address,
    string City,
    string State,
    string PostalCode,
    string Phone,
    string Email,
    DateTimeOffset ExpirationDate,

    string? TradeName = null,
    string? Notes = null,

    DatabaseProvider? DatabaseProvider = null,
    string? DatabaseConnectionString = null
) : ICommand;