using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionUserEventHandler :
    IEventHandler<DomainEvents.UserRegistrationCompleted>;

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
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project user registration: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project user registration: {@event.Id}.", ex);
        }
    }
}
