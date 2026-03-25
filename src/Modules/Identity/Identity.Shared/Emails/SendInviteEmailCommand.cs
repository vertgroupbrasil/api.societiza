using Core.Application.Messaging.Commands;

namespace Identity.Shared.Emails;

public sealed record SendInviteEmailCommand(
    string ToEmail,
    string ToName,
    string InviteLink
) : ICommand;