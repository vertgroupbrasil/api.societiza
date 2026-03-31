using Core.Application.Messaging.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands;

namespace Identity.Application.UseCases.Commands;

public sealed class AcceptInviteHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.UserInvite> inviteProjection,
    IPasswordService passwordService
) : ICommandHandler<AcceptInviteCommand>
{
    public async Task<Result> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        var inviteProjectionModel = await inviteProjection.FindAsync(
            x => x.TokenHash == request.Token,
            cancellationToken);

        if (inviteProjectionModel is null)
            return Result.Failure(new NotFoundError(new("Invite.NotFound", "Invite not found or token is invalid")));

        var inviteResult = await applicationService.LoadAggregateAsync<UserInvite>(inviteProjectionModel.Id, cancellationToken);

        if (!inviteResult.IsSuccess)
            return Result.Failure(inviteResult.Error);

        var invite = inviteResult.Value;

        if (invite.Status != InviteStatus.Pending)
            return Result.Failure(new ConflictError(new("Invite.NotPending", "Invite is not in a pending state")));

        if (invite.ExpiresAt <= DateTimeOffset.UtcNow)
            return Result.Failure(new Error("Invite.Expired", "Invite has expired"));

        var passwordHash = passwordService.HashPassword(request.Password);

        var user = User.Create(
            inviteId: invite.Id,
            firstName: request.FirstName,
            lastName: request.LastName,
            email: invite.Email,
            passwordHash: passwordHash,
            role: invite.Role,
            accountancyId: invite.AccountancyId
        );

        invite.Accept();

        await applicationService.AppendEventsAsync(user, cancellationToken);
        await applicationService.AppendEventsAsync(invite, cancellationToken);

        return Result.Success();
    }
}