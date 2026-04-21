using Core.Application.Messaging.Commands;

namespace Identity.Shared.Jobs;

public sealed record ExpireInvitationLinksCommand() : ICommand;