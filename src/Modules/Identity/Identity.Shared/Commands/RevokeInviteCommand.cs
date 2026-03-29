using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands;

public sealed record RevokeInviteCommand(
    Guid InviteId
) : ICommand;