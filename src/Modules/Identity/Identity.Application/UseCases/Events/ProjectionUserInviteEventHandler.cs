using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionUserInviteEventHandler :
    IEventHandler<DomainEvents.UserInviteCreated>,
    IEventHandler<DomainEvents.UserInviteResent>,
    IEventHandler<DomainEvents.UserInviteAccepted>,
    IEventHandler<DomainEvents.UserInviteRevoked>,
    IEventHandler<DomainEvents.UserInviteExpired>;

public class ProjectionUserInviteEventHandler(
    IIdentityProjection<ProjectionModel.UserInvite> inviteProjection,
    ILogger<ProjectionUserInviteEventHandler> logger
) : IProjectionUserInviteEventHandler
{
    public async Task Handle(DomainEvents.UserInviteCreated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await inviteProjection.ReplaceInsertAsync(new(
                @event.Id,
                @event.Email,
                @event.Role,
                @event.AccountancyId,
                @event.TokenHash,
                @event.Status,
                @event.ExpiresAt,
                null,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invite creation: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invite creation: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserInviteResent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await inviteProjection.FindAsync(x => x.Id == @event.Id, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("Invite projection not found for resend: {Id}.", @event.Id);
                return;
            }

            await inviteProjection.ReplaceInsertAsync(existing with
            {
                TokenHash = @event.TokenHash,
                ExpiresAt = @event.ExpiresAt,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invite resend: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invite resend: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserInviteAccepted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await inviteProjection.FindAsync(x => x.Id == @event.Id, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("Invite projection not found for acceptance: {Id}.", @event.Id);
                return;
            }

            await inviteProjection.ReplaceInsertAsync(existing with
            {
                Status = @event.Status,
                UsedAt = @event.UsedAt,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invite acceptance: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invite acceptance: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserInviteRevoked @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await inviteProjection.FindAsync(x => x.Id == @event.Id, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("Invite projection not found for revocation: {Id}.", @event.Id);
                return;
            }

            await inviteProjection.ReplaceInsertAsync(existing with
            {
                Status = @event.Status,
                UsedAt = @event.UsedAt,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invite revocation: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invite revocation: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserInviteExpired @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await inviteProjection.FindAsync(x => x.Id == @event.Id, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("Invite projection not found for expiration: {Id}.", @event.Id);
                return;
            }

            await inviteProjection.ReplaceInsertAsync(existing with
            {
                Status = @event.Status,
                UsedAt = @event.UsedAt,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invite expiration: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invite expiration: {@event.Id}.", ex);
        }
    }
}
