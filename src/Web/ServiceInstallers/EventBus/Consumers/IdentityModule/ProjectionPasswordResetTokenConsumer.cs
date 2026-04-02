using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionPasswordResetTokenConsumer(IProjectionPasswordResetTokenEventHandler eventHandler) :
    IConsumer<DomainEvents.UserPasswordResetTokenAdded>,
    IConsumer<DomainEvents.UserPasswordResetTokenUsed>
{
    public Task Consume(ConsumeContext<DomainEvents.UserPasswordResetTokenAdded> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserPasswordResetTokenUsed> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}
