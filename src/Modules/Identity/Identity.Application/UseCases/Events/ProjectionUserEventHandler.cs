using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionUserEventHandler :
    IEventHandler<DomainEvents.UserInvited>,
    IEventHandler<DomainEvents.UserRegistrationCompleted>;

public class ProjectionUserEventHandler(
    IIdentityProjection<ProjectionModel.User> userProjection,
    ILogger<ProjectionUserEventHandler> logger
) : IProjectionUserEventHandler
{
    public async Task Handle(DomainEvents.UserInvited @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await userProjection.ReplaceInsertAsync(new(
                @event.Id,
                @event.Name,
                @event.Email,
                @event.Role,
                @event.Status,
                @event.AccountancyId,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project invited user: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project invited user: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserRegistrationCompleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await userProjection.FindAsync(x => x.Id == @event.Id, cancellationToken);

            if (existing is null)
            {
                logger.LogWarning("User projection not found for registration completion: {Id}.", @event.Id);
                return;
            }

            await userProjection.ReplaceInsertAsync(existing with
            {
                Status = @event.Status,
                UpdatedAt = @event.UpdatedAt
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project registration completion for user: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project registration completion for user: {@event.Id}.", ex);
        }
    }
}