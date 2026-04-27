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

public sealed class GetUserByIdHandler(
    IIdentityProjection<ProjectionModel.User> userProjection,
    ICurrentUserService currentUser
) : IQueryHandler<GetUserByIdQuery, UserProfileResponse>
{
    public async Task<Result<UserProfileResponse>> Handle(
        GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        if (!UserRole.TryFromName(currentUser.Role, out var callerRole))
            return Result.Failure<UserProfileResponse>(
                new ForbiddenError(new("User.Forbidden", "You do not have permission to view this user.")));

        if (callerRole == UserRole.AccountancyEmployee && request.UserId != currentUser.UserId)
            return Result.Failure<UserProfileResponse>(
                new ForbiddenError(new("User.Forbidden", "You do not have permission to view this user.")));

        var target = await userProjection.FindAsync(
            x => x.Id == request.UserId && !x.IsDeleted, cancellationToken);

        if (target is null)
            return Result.Failure<UserProfileResponse>(
                new NotFoundError(new("User.NotFound", "User not found.")));

        if (!UserRole.TryFromName(target.Role, out var targetRole))
            return Result.Failure<UserProfileResponse>(
                new NotFoundError(new("User.NotFound", "User not found.")));

        var hasAccess = callerRole switch
        {
            _ when callerRole == UserRole.SystemAdmin =>
                targetRole == UserRole.SystemAdmin
                || targetRole == UserRole.AccountancyAdmin
                || targetRole == UserRole.AccountancyEmployee,

            _ when callerRole == UserRole.AccountancyAdmin =>
                target.AccountancyId == currentUser.AccountancyId
                && (targetRole == UserRole.AccountancyAdmin || targetRole == UserRole.AccountancyEmployee),

            _ when callerRole == UserRole.AccountancyEmployee =>
                target.Id == currentUser.UserId,

            _ => false,
        };

        if (!hasAccess)
            return Result.Failure<UserProfileResponse>(
                new ForbiddenError(new("User.Forbidden", "You do not have permission to view this user.")));

        return Result.Success(new UserProfileResponse(
            target.Id,
            target.FirstName,
            target.LastName,
            target.Email,
            target.Role,
            target.AccountancyId,
            target.CreatedAt,
            target.UpdatedAt));
    }
}
