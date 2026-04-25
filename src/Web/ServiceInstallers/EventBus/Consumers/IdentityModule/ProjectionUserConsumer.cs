using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionUserConsumer(IProjectionUserEventHandler eventHandler) :
    IConsumer<DomainEvents.UserRegistrationCompleted>,
    IConsumer<DomainEvents.UserPasswordUpdated>,
    IConsumer<DomainEvents.UserNameUpdated>,
    IConsumer<DomainEvents.UserPromotedToAccountancyAdmin>,
    IConsumer<DomainEvents.UserDeleted>
{
    public Task Consume(ConsumeContext<DomainEvents.UserRegistrationCompleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserPasswordUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserNameUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserPromotedToAccountancyAdmin> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserDeleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}
