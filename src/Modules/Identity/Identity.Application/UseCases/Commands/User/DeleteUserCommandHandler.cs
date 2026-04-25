using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands.User;

namespace Identity.Application.UseCases.Commands.User;

using UserAggregate = Identity.Domain.Aggregates.User;

public sealed class DeleteUserCommandHandler(
    IIdentityApplicationService applicationService,
    ICurrentUserService currentUser,
    IIdentityProjection<ProjectionModel.User> userProjection
) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var callerRole = UserRole.FromName(currentUser.Role);

        if (callerRole != UserRole.SystemAdmin && callerRole != UserRole.AccountancyAdmin)
            return Result.Failure(new ForbiddenError(new(
                "User.Forbidden", "You do not have permission to remove users.")));

        var userResult = await applicationService.LoadAggregateAsync<UserAggregate>(
            request.UserId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure(userResult.Error);

        var targetUser = userResult.Value;

        if (targetUser.Id == currentUser.UserId)
            return Result.Failure(new ConflictError(new(
                "User.CannotRemoveSelf", "Cannot remove yourself.")));

        if (callerRole == UserRole.AccountancyAdmin && targetUser.AccountancyId != currentUser.AccountancyId)
            return Result.Failure(new ForbiddenError(new(
                "User.ForbiddenTenant", "You can only manage members of your own accountancy.")));

        if (targetUser.Role == UserRole.AccountancyAdmin && targetUser.AccountancyId is not null)
        {
            var adminCount = (await userProjection.ListAsync(
                x => x.AccountancyId == targetUser.AccountancyId
                  && x.Role == UserRole.AccountancyAdmin.Name
                  && !x.IsDeleted,
                cancellationToken)).Count;

            if (adminCount <= 1)
                return Result.Failure(new ConflictError(new(
                    "User.LastAccountancyAdmin", "Cannot remove the last administrator of the accountancy.")));
        }

        if (targetUser.Role == UserRole.SystemAdmin)
        {
            var adminCount = (await userProjection.ListAsync(
                x => x.Role == UserRole.SystemAdmin.Name && !x.IsDeleted,
                cancellationToken)).Count;

            if (adminCount <= 1)
                return Result.Failure(new ConflictError(new(
                    "User.LastSystemAdmin", "Cannot remove the last system administrator.")));
        }

        targetUser.Delete(currentUser.UserId);

        await applicationService.AppendEventsAsync(targetUser, cancellationToken);

        return Result.Success();
    }
}
