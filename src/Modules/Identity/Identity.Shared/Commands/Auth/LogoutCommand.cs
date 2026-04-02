using Core.Application.Messaging.Commands;

namespace Identity.Shared.Commands.Auth;

public sealed record LogoutCommand(string RefreshToken) : ICommand;
