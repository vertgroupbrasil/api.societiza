using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Aggregates;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Jobs;

public sealed record ExpireInvitesCommand() : ICommand;

internal sealed class ExpireInvitesHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.UserInvite> inviteProjection,
    ILogger<ExpireInvitesHandler> logger
) : ICommandHandler<ExpireInvitesCommand>
{
    public async Task<Result> Handle(ExpireInvitesCommand request, CancellationToken cancellationToken)
    {
        var expiredInvites = await inviteProjection.ListAsync(
            x => x.Status == InviteStatus.Pending.Name && x.ExpiresAt < DateTimeOffset.UtcNow,
            cancellationToken);

        foreach (var expired in expiredInvites)
        {
            var inviteResult = await applicationService.LoadAggregateAsync<UserInvite>(expired.Id, cancellationToken);

            if (!inviteResult.IsSuccess)
            {
                logger.LogWarning("Failed to load invite aggregate for expiration: {Id}.", expired.Id);
                continue;
            }

            inviteResult.Value.Expire();

            await applicationService.AppendEventsAsync(inviteResult.Value, cancellationToken);
            logger.LogInformation("Expired invite: {Id}.", expired.Id);
        }

        return Result.Success();
    }
}