using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionRefreshTokenConsumer(IProjectionRefreshTokenEventHandler eventHandler) :
    IConsumer<DomainEvents.UserRefreshTokenAdded>,
    IConsumer<DomainEvents.UserRefreshTokenRevoked>
{
    public Task Consume(ConsumeContext<DomainEvents.UserRefreshTokenAdded> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserRefreshTokenRevoked> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}
