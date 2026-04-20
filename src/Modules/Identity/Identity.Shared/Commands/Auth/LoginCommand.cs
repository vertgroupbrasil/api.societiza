using Core.Application.Messaging.Commands;
using Identity.Shared.Response.Auth;

namespace Identity.Shared.Commands.Auth;

public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<LoginResponse>;