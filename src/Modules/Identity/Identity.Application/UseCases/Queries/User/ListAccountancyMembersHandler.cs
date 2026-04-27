using Core.Application.Messaging.Queries;
using Core.Application.Pagination;
using Core.Application.Services.Interfaces;
using Core.Domain.Pagination;
using Core.Domain.Pagination.Interfaces;
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
) : IQueryHandler<ListAccountancyMembersQuery, IPagedResult<object>>
{
    public async Task<Result<IPagedResult<object>>> Handle(ListAccountancyMembersQuery request, CancellationToken cancellationToken)
    {
        var callerRole = UserRole.FromName(currentUser.Role);

        if (callerRole != UserRole.SystemAdmin && currentUser.AccountancyId != request.AccountancyId)
            return Result.Failure<IPagedResult<object>>(new ForbiddenError(new(
                "User.ForbiddenTenant", "You can only list members of your own accountancy.")));

        var members = await userProjection.ListAsync(
            x => x.AccountancyId == request.AccountancyId && (!request.OnlyActive || !x.IsDeleted),
            cancellationToken);

        var ordered = members
            .OrderBy(x => UserRole.TryFromName(x.Role, out var r) ? r.Value : int.MaxValue)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        var paging = new Paging(request.PageSize, request.PageNumber);

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
                .Cast<object>()
                .AsQueryable();

            return Result.Success(PagedResult<object>.Create(paging, details));
        }

        var summaries = ordered
            .Select(x => new UserSummaryResponse(x.Id, x.FirstName, x.LastName, x.Role))
            .Cast<object>()
            .AsQueryable();

        return Result.Success(PagedResult<object>.Create(paging, summaries));
    }
}
