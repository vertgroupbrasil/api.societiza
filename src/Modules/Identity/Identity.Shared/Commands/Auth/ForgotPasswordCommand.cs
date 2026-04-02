using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.Auth;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
