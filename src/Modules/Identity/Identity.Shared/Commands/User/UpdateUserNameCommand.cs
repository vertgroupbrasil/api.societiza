using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.User;

public sealed record UpdateUserNameCommand(string FirstName, string LastName) : ICommand;