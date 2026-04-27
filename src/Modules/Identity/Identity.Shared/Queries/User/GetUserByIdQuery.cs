using Core.Application.Messaging.Queries;
using Identity.Shared.Response.User;

namespace Identity.Shared.Queries.User;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserProfileResponse>;
