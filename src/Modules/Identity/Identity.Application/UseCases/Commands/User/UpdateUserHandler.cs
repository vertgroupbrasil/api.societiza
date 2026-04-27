using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Shared.Commands.User;

namespace Identity.Application.UseCases.Commands.User;

using UserAggregate = Identity.Domain.Aggregates.User;

public sealed class UpdateUserHandler(
    IIdentityApplicationService applicationService,
    ICurrentUserService currentUser
) : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var userResult = await applicationService.LoadAggregateAsync<UserAggregate>(
            currentUser.UserId, cancellationToken);

        if (!userResult.IsSuccess)
            return Result.Failure(new NotFoundError(new("User.NotFound", "User not found.")));

        userResult.Value.Update(request.FirstName, request.LastName);

        await applicationService.AppendEventsAsync(userResult.Value, cancellationToken);
        return Result.Success();
    }
}