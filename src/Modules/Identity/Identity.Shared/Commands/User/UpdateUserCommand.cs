using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.User;

public sealed record UpdateUserCommand(string FirstName, string LastName) : ICommand;