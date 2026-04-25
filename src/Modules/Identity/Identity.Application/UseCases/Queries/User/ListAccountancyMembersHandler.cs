using Core.Application.Messaging.Queries;
using Core.Application.Services.Interfaces;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Queries.User;
using Identity.Shared.Response.User;

namespace Identity.Application.UseCases.Queries.User;

public sealed class ListAccountancyMembersHandler(
    IIdentityProjection<ProjectionModel.User> userProjection,
    ICurrentUserService currentUser
) : IQueryHandler<ListAccountancyMembersQuery, AccountancyMembersResponse>
{
    public async Task<Result<AccountancyMembersResponse>> Handle(
        ListAccountancyMembersQuery request, CancellationToken cancellationToken)
    {
        var callerRole = UserRole.FromName(currentUser.Role);

        if (callerRole != UserRole.SystemAdmin && currentUser.AccountancyId != request.AccountancyId)
            return Result.Failure<AccountancyMembersResponse>(new ForbiddenError(new(
                "User.ForbiddenTenant", "You can only list members of your own accountancy.")));

        var members = await userProjection.ListAsync(
            x => x.AccountancyId == request.AccountancyId && !x.IsDeleted, cancellationToken);

        var ordered = members
            .OrderBy(x => UserRole.TryFromName(x.Role, out var r) ? r.Value : int.MaxValue)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        if (callerRole == UserRole.AccountancyAdmin || callerRole == UserRole.SystemAdmin)
        {
            var details = ordered
                .Select(x => new UserDetailResponse(
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.Email,
                    x.Role,
                    x.CreatedAt,
                    x.InvitationLinkId))
                .ToList();

            return Result.Success(new AccountancyMembersResponse(Members: details, Summaries: null));
        }

        var summaries = ordered
            .Select(x => new UserSummaryResponse(x.Id, x.FirstName, x.LastName, x.Role))
            .ToList();

        return Result.Success(new AccountancyMembersResponse(Members: null, Summaries: summaries));
    }
}
