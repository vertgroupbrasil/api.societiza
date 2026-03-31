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
    private const int MaxDegreeOfParallelism = 5;

    public async Task<Result> Handle(ExpireInvitesCommand request, CancellationToken cancellationToken)
    {
        var expiredInvites = await inviteProjection.ListAsync(
            x => x.Status == InviteStatus.Pending.Name && x.ExpiresAt < DateTimeOffset.UtcNow,
            cancellationToken);

        int total = 0, succeeded = 0, failed = 0;

        await Parallel.ForEachAsync(expiredInvites,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (expired, ct) =>
            {
                Interlocked.Increment(ref total);

                var inviteResult = await applicationService.LoadAggregateAsync<UserInvite>(expired.Id, ct);

                if (!inviteResult.IsSuccess)
                {
                    logger.LogWarning("Failed to load invite aggregate for expiration: {Id}.", expired.Id);
                    Interlocked.Increment(ref failed);
                    return;
                }

                if (!inviteResult.Value.IsValid())
                {
                    logger.LogDebug("Invite {Id} is no longer in a valid state for expiration, skipping.", expired.Id);
                    return;
                }

                inviteResult.Value.Expire();

                await applicationService.AppendEventsAsync(inviteResult.Value, ct);
                Interlocked.Increment(ref succeeded);
            });

        logger.LogInformation(
            "Invite expiration job completed. Processed: {Total}, Succeeded: {Succeeded}, Failed: {Failed}.",
            total, succeeded, failed);

        return Result.Success();
    }
}
