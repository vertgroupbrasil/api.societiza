using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands.User;
using Identity.Shared.Response.User;

namespace Identity.Application.UseCases.Commands.User;

using UserAggregate = Identity.Domain.Aggregates.User;

public sealed class PromoteUserHandler(
    IIdentityApplicationService applicationService,
    ICurrentUserService currentUser,
    IIdentityProjection<ProjectionModel.User> userProjection
) : ICommandHandler<PromoteUserCommand, UserDetailResponse>
{
    public async Task<Result<UserDetailResponse>> Handle(PromoteUserCommand request, CancellationToken cancellationToken)
    {
        var callerRole = UserRole.FromName(currentUser.Role);

        if (callerRole == UserRole.SystemAdmin)
            return Result.Failure<UserDetailResponse>(new ForbiddenError(new(
                "User.ForbiddenForSystemAdmin", "System administrators cannot promote members.")));

        if (callerRole != UserRole.AccountancyAdmin)
            return Result.Failure<UserDetailResponse>(new ForbiddenError(new(
                "User.Forbidden", "Only an accountancy administrator can promote members.")));

        if (currentUser.AccountancyId != request.AccountancyId)
            return Result.Failure<UserDetailResponse>(new ForbiddenError(new(
                "User.ForbiddenTenant", "You can only manage members of your own accountancy.")));

        var userResult = await applicationService.LoadAggregateAsync<UserAggregate>(request.UserId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure<UserDetailResponse>(new NotFoundError(new("User.NotFound", "User not found.")));

        var user = userResult.Value;

        if (user.AccountancyId != request.AccountancyId)
            return Result.Failure<UserDetailResponse>(new ForbiddenError(new(
                "User.ForbiddenTenant", "You can only manage members of your own accountancy.")));

        if (user.Role != UserRole.AccountancyEmployee)
            return Result.Failure<UserDetailResponse>(new ConflictError(new(
                "User.AlreadyAdmin", "User is already an accountancy administrator.")));

        var snapshot = await userProjection.FindAsync(x => x.Id == user.Id, cancellationToken);

        user.Promote(currentUser.UserId);
        await applicationService.AppendEventsAsync(user, cancellationToken);

        return Result.Success(new UserDetailResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role.Name,
            user.CreatedAt,
            snapshot?.InvitationLinkId));
    }
}