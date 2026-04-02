using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionUserConsumer(IProjectionUserEventHandler eventHandler) :
    IConsumer<DomainEvents.UserRegistrationCompleted>,
    IConsumer<DomainEvents.UserPasswordUpdated>
{
    public Task Consume(ConsumeContext<DomainEvents.UserRegistrationCompleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserPasswordUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}
