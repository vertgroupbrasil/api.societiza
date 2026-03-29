using Core.Application.Messaging.Commands;
using Core.Shared.Response;

namespace Identity.Shared.Commands;

public sealed record InviteUserCommand(
    string Email,
    string Role,
    Guid? AccountancyId
) : ICommand<IdentifierResponse>;