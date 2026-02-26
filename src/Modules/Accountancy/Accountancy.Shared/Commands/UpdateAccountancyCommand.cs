using Core.Application.Messaging.Commands;

namespace Accountancy.Shared.Commands;

public sealed record UpdateAccountancyCommand(
    Guid Id,
    string CNPJ,
    string LegalName,
    string? TradeName,
    string Address,
    string City,
    string State,
    string PostalCode,
    string Phone,
    string? Email
) : ICommand;