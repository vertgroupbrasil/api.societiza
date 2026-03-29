using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Options;
using Core.Shared.Errors;
using Core.Shared.Response;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Commands;
using Identity.Shared.Emails;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.Commands;

public class InviteUserHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.UserInvite> inviteProjection,
    IIdentityProjection<ProjectionModel.User> userProjection,
    IJobSchedulerService jobScheduler,
    IOptions<FrontendOptions> frontendOptions
) : ICommandHandler<InviteUserCommand, IdentifierResponse>
{
    public async Task<Result<IdentifierResponse>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var pendingInvite = await inviteProjection.FindAsync(
            x => x.Email == request.Email && x.Status == InviteStatus.Pending.Name,
            cancellationToken);

        if (pendingInvite is not null)
            return Result.Failure<IdentifierResponse>(new ConflictError(new(
                "Invite.AlreadyPending", "There is already a pending invite for this email")));

        var existingUser = await userProjection.FindAsync(x => x.Email == request.Email, cancellationToken);

        if (existingUser is not null)
            return Result.Failure<IdentifierResponse>(new ConflictError(new(
                "User.AlreadyExists", "A user with this email already exists")));

        UserRole role = request.Role;
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var invite = UserInvite.Create(
            email: request.Email,
            role: role,
            accountancyId: request.AccountancyId,
            tokenHash: tokenHash
        );

        await applicationService.AppendEventsAsync(invite, cancellationToken);

        var inviteLink = $"{frontendOptions.Value.BaseUrl}/auth/accept-invite?token={rawToken}";

        jobScheduler.Enqueue(new SendInviteEmailCommand(
            ToEmail: request.Email,
            ToName: request.Email,
            InviteLink: inviteLink
        ));

        return Result.Success(new IdentifierResponse(invite.Id));
    }
}