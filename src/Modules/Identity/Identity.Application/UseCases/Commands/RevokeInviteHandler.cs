using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Enumerations;
using Identity.Shared.Commands;

namespace Identity.Application.UseCases.Commands;

public sealed class RevokeInviteHandler(
    IIdentityApplicationService applicationService
) : ICommandHandler<RevokeInviteCommand>
{
    public async Task<Result> Handle(RevokeInviteCommand request, CancellationToken cancellationToken)
    {
        var inviteResult = await applicationService.LoadAggregateAsync<UserInvite>(request.InviteId, cancellationToken);

        if (!inviteResult.IsSuccess)
            return Result.Failure(new NotFoundError(new("Invite.NotFound", "Invite not found")));

        var invite = inviteResult.Value;

        if (invite.Status != InviteStatus.Pending)
            return Result.Failure(new ConflictError(new("Invite.NotRevokable", "Only pending invites can be revoked")));

        invite.Revoke();

        await applicationService.AppendEventsAsync(invite, cancellationToken);
        return Result.Success();
    }
}