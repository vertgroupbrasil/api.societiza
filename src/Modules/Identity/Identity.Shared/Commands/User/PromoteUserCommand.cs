using Core.Application.Messaging.Commands;
using Identity.Shared.Response.User;

namespace Identity.Shared.Commands.User;

public sealed record PromoteUserCommand(Guid AccountancyId, Guid UserId) : ICommand<UserDetailResponse>;
