using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionPasswordResetTokenEventHandler :
    IEventHandler<DomainEvents.UserPasswordResetTokenAdded>,
    IEventHandler<DomainEvents.UserPasswordResetTokenUsed>;

public class ProjectionPasswordResetTokenEventHandler(
    IIdentityProjection<ProjectionModel.PasswordResetToken> passwordResetTokenProjection,
    ILogger<ProjectionPasswordResetTokenEventHandler> logger
) : IProjectionPasswordResetTokenEventHandler
{
    public async Task Handle(DomainEvents.UserPasswordResetTokenAdded @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await passwordResetTokenProjection.ReplaceInsertAsync(new(
                @event.TokenId,
                @event.UserId,
                @event.TokenHash,
                @event.ExpiresAt,
                IsUsed: false,
                UsedAt: null,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project password reset token added: {Id}.", @event.TokenId);
            throw new InvalidOperationException($"Failed to project password reset token added: {@event.TokenId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserPasswordResetTokenUsed @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await passwordResetTokenProjection.UpdateFieldsAsync(@event.TokenId, builder => builder
                .Set(x => x.IsUsed, true)
                .Set(x => x.UsedAt, @event.UsedAt)
                .Set(x => x.UpdatedAt, @event.UsedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project password reset token used: {Id}.", @event.TokenId);
            throw new InvalidOperationException($"Failed to project password reset token used: {@event.TokenId}.", ex);
        }
    }
}