using Accountancy.Domain.Events;
using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Core.Application.EventBus;
using Microsoft.Extensions.Logging;

namespace Accountancy.Application.UseCases.Events;

public interface IProjectionAccountancyEventHandler :
    IEventHandler<DomainEvents.AccountancyCreated>,
    IEventHandler<DomainEvents.AccountancyUpdated>,
    IEventHandler<DomainEvents.AccountancyDeleted>;

public class ProjectionAccountancyEventHandler(
    IAccountancyProjection<ProjectionModel.Accountancy> accountancyProjection,
    ILogger<ProjectionAccountancyEventHandler> logger) : IProjectionAccountancyEventHandler
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
            await accountancyProjection.UpdateFieldsAsync(@event.Id, builder => builder
                .Set(x => x.CNPJ, @event.CNPJ)
                .Set(x => x.LegalName, @event.LegalName)
                .Set(x => x.TradeName, @event.TradeName)
                .Set(x => x.Address, @event.Address)
                .Set(x => x.City, @event.City)
                .Set(x => x.State, @event.State)
                .Set(x => x.PostalCode, @event.PostalCode)
                .Set(x => x.Phone, @event.Phone)
                .Set(x => x.Email, @event.Email)
                .Set(x => x.UpdatedAt, @event.UpdatedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update accountancy: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to update accountancy: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.AccountancyDeleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await accountancyProjection.UpdateOneFieldAsync(
                @event.Id,
                x => x.IsDeleted,
                true,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete accountancy: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to delete accountancy: {@event.Id}.", ex);
        }
    }
}