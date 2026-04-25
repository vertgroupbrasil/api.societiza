using Core.Application.Messaging.Queries;
using Core.Application.Services.Interfaces;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Queries.User;
using Identity.Shared.Response.User;

namespace Identity.Application.UseCases.Queries.User;

public sealed class GetMyProfileHandler(
    IIdentityProjection<ProjectionModel.User> userProjection,
    ICurrentUserService currentUser
) : IQueryHandler<GetMyProfileQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userView = await userProjection.FindAsync(
            x => x.Id == currentUser.UserId && !x.IsDeleted, cancellationToken);

        if (userView is null)
            return Result.Failure<UserProfileResponse>(
                new NotFoundError(new("User.NotFound", "User not found.")));

        return Result.Success(new UserProfileResponse(
            userView.Id,
            userView.FirstName,
            userView.LastName,
            userView.Email,
            userView.Role,
            userView.AccountancyId,
            userView.CreatedAt,
            userView.UpdatedAt));
    }
}
