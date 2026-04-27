using Core.Application.Messaging.Queries;
using Core.Application.Pagination;
using Core.Domain.Pagination;
using Core.Domain.Pagination.Interfaces;
using Core.Shared.Results;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Queries.User;
using Identity.Shared.Response.User;

namespace Identity.Application.UseCases.Queries.User;

public sealed class ListSystemAdminsHandler(
    IIdentityProjection<ProjectionModel.User> userProjection
) : IQueryHandler<ListSystemAdminsQuery, IPagedResult<UserDetailResponse>>
{
    public async Task<Result<IPagedResult<UserDetailResponse>>> Handle(ListSystemAdminsQuery request, CancellationToken cancellationToken)
    {
        var admins = await userProjection.ListAsync(
            x => x.Role == UserRole.SystemAdmin.Name && (!request.OnlyActive || !x.IsDeleted),
            cancellationToken);

        var response = admins
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new UserDetailResponse(
                x.Id,
                x.FirstName,
                x.LastName,
                x.Email,
                x.Role,
                x.CreatedAt,
                x.InvitationLinkId))
            .AsQueryable();

        var paging = new Paging(request.PageSize, request.PageNumber);
        return Result.Success(PagedResult<UserDetailResponse>.Create(paging, response));
    }
}
