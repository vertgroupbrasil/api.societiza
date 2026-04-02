using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionRefreshTokenEventHandler :
    IEventHandler<DomainEvents.UserRefreshTokenAdded>,
    IEventHandler<DomainEvents.UserRefreshTokenRevoked>;

public class ProjectionRefreshTokenEventHandler(
    IIdentityProjection<ProjectionModel.RefreshToken> refreshTokenProjection,
    ILogger<ProjectionRefreshTokenEventHandler> logger
) : IProjectionRefreshTokenEventHandler
{
    public async Task Handle(DomainEvents.UserRefreshTokenAdded @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await refreshTokenProjection.ReplaceInsertAsync(new(
                @event.TokenId,
                @event.UserId,
                @event.TokenHash,
                @event.ExpiresAt,
                IsRevoked: false,
                RevokedAt: null,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project refresh token added: {Id}.", @event.TokenId);
            throw new InvalidOperationException($"Failed to project refresh token added: {@event.TokenId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserRefreshTokenRevoked @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await refreshTokenProjection.UpdateFieldsAsync(@event.TokenId, builder => builder
                .Set(x => x.IsRevoked, true)
                .Set(x => x.RevokedAt, @event.RevokedAt)
                .Set(x => x.UpdatedAt, @event.RevokedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project refresh token revoked: {Id}.", @event.TokenId);
            throw new InvalidOperationException($"Failed to project refresh token revoked: {@event.TokenId}.", ex);
        }
    }
}
