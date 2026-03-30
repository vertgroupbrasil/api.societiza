using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands;

public sealed record AcceptInviteCommand(
    string Token,
    string FirstName,
    string LastName,
    string Password,
    string ConfirmPassword
) : ICommand;