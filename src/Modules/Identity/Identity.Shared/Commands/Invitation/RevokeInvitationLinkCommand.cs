using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.Invitation;

public sealed record RevokeInvitationLinkCommand(
    Guid InvitationLinkId
) : ICommand;