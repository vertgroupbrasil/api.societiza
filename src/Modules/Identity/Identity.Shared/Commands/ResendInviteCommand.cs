using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands;

public sealed record ResendInviteCommand(
    Guid InviteId
) : ICommand;