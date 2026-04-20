using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.Auth;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword
) : ICommand;