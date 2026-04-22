using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionInvitationLinkEventHandler :
    IEventHandler<DomainEvents.InvitationLinkCreated>,
    IEventHandler<DomainEvents.InvitationLinkConsumed>,
    IEventHandler<DomainEvents.InvitationLinkExhausted>,
    IEventHandler<DomainEvents.InvitationLinkExpired>;

public class ProjectionInvitationLinkEventHandler(
    IIdentityProjection<ProjectionModel.InvitationLink> linkProjection,
    ILogger<ProjectionInvitationLinkEventHandler> logger
) : IProjectionInvitationLinkEventHandler
{
    public async Task Handle(DomainEvents.InvitationLinkCreated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await linkProjection.ReplaceInsertAsync(new(
                @event.Id,
                @event.TargetRole,
                @event.Status,
                @event.AccountancyId,
                @event.CreatedByUserId,
                @event.Token,
                @event.TokenHash,
                @event.MaxUses,
                Uses: 0,
                @event.ExpiresAt,
                @event.CreatedAt,
                UpdatedAt: null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invitation link creation: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invitation link creation: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.InvitationLinkConsumed @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await linkProjection.FindAsync(x => x.Id == @event.Id, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("Invitation link projection not found for consumption: {Id}.", @event.Id);
                return;
            }

            await linkProjection.ReplaceInsertAsync(existing with
            {
                Uses = @event.Uses,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invitation link consumption: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invitation link consumption: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.InvitationLinkExhausted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await linkProjection.FindAsync(x => x.Id == @event.Id, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("Invitation link projection not found for exhaustion: {Id}.", @event.Id);
                return;
            }

            await linkProjection.ReplaceInsertAsync(existing with
            {
                Status = @event.Status,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invitation link exhaustion: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invitation link exhaustion: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.InvitationLinkExpired @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await linkProjection.FindAsync(x => x.Id == @event.Id, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("Invitation link projection not found for expiration: {Id}.", @event.Id);
                return;
            }

            await linkProjection.ReplaceInsertAsync(existing with
            {
                Status = @event.Status,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invitation link expiration: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invitation link expiration: {@event.Id}.", ex);
        }
    }
}