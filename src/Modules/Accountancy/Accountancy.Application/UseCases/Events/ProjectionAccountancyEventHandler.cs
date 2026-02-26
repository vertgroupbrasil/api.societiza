using Accountancy.Domain.Events;
using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Core.Application.EventBus;
using DnsClient.Internal;

namespace Accountancy.Application.UseCases.Events;

public interface IProjectionAccountancyEventHandler :
    IEventHandler<DomainEvents.AccountancyCreated>,
    IEventHandler<DomainEvents.AccountancyUpdated>;

public class ProjectionAccountancyEventHandler(
    IAccountancyProjection<ProjectionModel.Accountancy> accountancyProjection,
    ILogger logger) : IProjectionAccountancyEventHandler
{
    public async Task Handle(DomainEvents.AccountancyCreated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await accountancyProjection.ReplaceInsertAsync(new(
                @event.Id,
                @event.CNPJ,
                @event.LegalName,
                @event.TradeName,
                @event.Address,
                @event.City,
                @event.State,
                @event.PostalCode,
                @event.Phone,
                @event.Email,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create accountancy: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to create accountancy: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.AccountancyUpdated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await accountancyProjection.ReplaceInsertAsync(new(
                @event.Id,
                @event.CNPJ,
                @event.LegalName,
                @event.TradeName,
                @event.Address,
                @event.City,
                @event.State,
                @event.PostalCode,
                @event.Phone,
                @event.Email,
                @event.CreatedAt,
                @event.UpdatedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update accountancy: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to update accountancy: {@event.Id}.", ex);
        }
    }
}