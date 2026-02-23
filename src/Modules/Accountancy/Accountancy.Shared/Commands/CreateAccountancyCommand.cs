using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace Accountancy.Shared.Commands;

public sealed record CreateAccountancyCommand(
    string CNPJ,
    string LegalName,
    string? TradeName,
    string Address,
    string City,
    string State,
    string PostalCode,
    string Phone,
    string? Email
) : ICommand<IdentifierResponse>;