using Core.Application.Messaging.Commands;

namespace Identity.Shared.Emails;

public sealed record SendPasswordResetEmailCommand(
    string ToEmail,
    string ToName,
    string ResetLink
) : ICommand;