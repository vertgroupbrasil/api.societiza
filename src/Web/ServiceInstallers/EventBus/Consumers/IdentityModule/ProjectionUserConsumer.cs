using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionUserConsumer(IProjectionUserEventHandler eventHandler) :
    IConsumer<DomainEvents.UserInvited>,
    IConsumer<DomainEvents.UserRegistrationCompleted>
{
    public Task Consume(ConsumeContext<DomainEvents.UserInvited> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserRegistrationCompleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}