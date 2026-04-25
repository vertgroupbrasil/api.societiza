using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.User;

public sealed record DeleteUserCommand(Guid UserId, Guid? AccountancyId) : ICommand;