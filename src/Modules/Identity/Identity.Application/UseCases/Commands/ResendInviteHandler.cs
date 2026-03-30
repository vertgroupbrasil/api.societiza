using System.Security.Cryptography;
using System.Text;
using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Infrastructure.Options;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Enumerations;
using Identity.Shared.Commands;
using Identity.Shared.Emails;
using Microsoft.Extensions.Options;

namespace Identity.Application.UseCases.Commands;

public sealed class ResendInviteHandler(
    IIdentityApplicationService applicationService,
    IJobSchedulerService jobScheduler,
    IOptions<FrontendOptions> frontendOptions
) : ICommandHandler<ResendInviteCommand>
{
    public async Task<Result> Handle(ResendInviteCommand request, CancellationToken cancellationToken)
    {
        var inviteResult = await applicationService.LoadAggregateAsync<UserInvite>(request.InviteId, cancellationToken);

        if (!inviteResult.IsSuccess)
            return Result.Failure(new NotFoundError(new("Invite.NotFound", "Invite not found")));

        var invite = inviteResult.Value;

        if (invite.Status != InviteStatus.Pending)
            return Result.Failure(new ConflictError(new("Invite.NotResendable", "Only pending invites can be resent")));

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var newTokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        invite.Resend(newTokenHash);

        await applicationService.AppendEventsAsync(invite, cancellationToken);

        var inviteLink = $"{frontendOptions.Value.BaseUrl}/auth/accept-invite?token={rawToken}";

        jobScheduler.Enqueue(new SendInviteEmailCommand(
            ToEmail: invite.Email,
            ToName: invite.Email,
            InviteLink: inviteLink
        ));

        return Result.Success();
    }
}