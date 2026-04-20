using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace Identity.Shared.Commands.User;

public sealed record RegisterUserCommand(
    string Token,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword
) : ICommand<IdentifierResponse>;