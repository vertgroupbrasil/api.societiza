using Core.Application.Messaging.Commands;
using Core.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Enumerations;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Identity.Shared.Jobs;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Jobs;

using InvitationLinkAggregate = Identity.Domain.Aggregates.InvitationLink;

internal sealed class ExpireInvitationLinksHandler(
    IIdentityApplicationService applicationService,
    IIdentityProjection<ProjectionModel.InvitationLink> linkProjection,
    ILogger<ExpireInvitationLinksHandler> logger
) : ICommandHandler<ExpireInvitationLinksCommand>
{
    public async Task<Result> Handle(ExpireInvitationLinksCommand request, CancellationToken cancellationToken)
    {
        var candidates = await linkProjection.ListAsync(
            x => x.Status == InvitationLinkStatus.Active.Name && x.ExpiresAt < DateTimeOffset.UtcNow,
            cancellationToken);

        foreach (var candidate in candidates)
        {
            var linkResult = await applicationService.LoadAggregateAsync<InvitationLinkAggregate>(
                candidate.Id, cancellationToken);

            if (!linkResult.IsSuccess)
            {
                logger.LogWarning("Failed to load invitation link for expiration: {Id}.", candidate.Id);
                continue;
            }

            linkResult.Value.Expire();
            await applicationService.AppendEventsAsync(linkResult.Value, cancellationToken);
        }

        return Result.Success();
    }
}