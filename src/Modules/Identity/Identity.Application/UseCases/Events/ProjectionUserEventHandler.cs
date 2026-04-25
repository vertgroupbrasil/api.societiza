using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionUserEventHandler :
    IEventHandler<DomainEvents.UserRegistrationCompleted>,
    IEventHandler<DomainEvents.UserPasswordUpdated>,
    IEventHandler<DomainEvents.UserNameUpdated>,
    IEventHandler<DomainEvents.UserPromotedToAccountancyAdmin>,
    IEventHandler<DomainEvents.UserDeleted>;

public class ProjectionUserEventHandler(
    IIdentityProjection<ProjectionModel.User> userProjection,
    ILogger<ProjectionUserEventHandler> logger
) : IProjectionUserEventHandler
{
    public async Task Handle(DomainEvents.UserRegistrationCompleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await userProjection.ReplaceInsertAsync(new(
                @event.Id,
                @event.FirstName,
                @event.LastName,
                @event.Email,
                @event.Role,
                @event.AccountancyId,
                @event.CreatedAt,
                null,
                @event.InvitationLinkId),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project user registration: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project user registration: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserPasswordUpdated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await userProjection.UpdateFieldsAsync(@event.Id, builder => builder
                .Set(x => x.UpdatedAt, @event.UpdatedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project password updated for user: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project password updated for user: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserNameUpdated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await userProjection.FindAsync(x => x.Id == @event.UserId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("User projection not found for name update: {Id}.", @event.UserId);
                return;
            }

            await userProjection.ReplaceInsertAsync(existing with
            {
                FirstName = @event.FirstName,
                LastName = @event.LastName,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project name update for user: {Id}.", @event.UserId);
            throw new InvalidOperationException($"Failed to project name update for user: {@event.UserId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserPromotedToAccountancyAdmin @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await userProjection.FindAsync(x => x.Id == @event.UserId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("User projection not found for promotion: {Id}.", @event.UserId);
                return;
            }

            await userProjection.ReplaceInsertAsync(existing with
            {
                Role = @event.Role,
                UpdatedAt = @event.PromotedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project promotion for user: {Id}.", @event.UserId);
            throw new InvalidOperationException($"Failed to project promotion for user: {@event.UserId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserDeleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await userProjection.FindAsync(x => x.Id == @event.UserId, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("User projection not found for deletion: {Id}.", @event.UserId);
                return;
            }

            await userProjection.ReplaceInsertAsync(existing with
            {
                IsDeleted = true,
                UpdatedAt = @event.DeletedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project deletion for user: {Id}.", @event.UserId);
            throw new InvalidOperationException($"Failed to project deletion for user: {@event.UserId}.", ex);
        }
    }
}