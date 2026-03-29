using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionUserInviteConsumer(IProjectionUserInviteEventHandler eventHandler) :
    IConsumer<DomainEvents.UserInviteCreated>,
    IConsumer<DomainEvents.UserInviteResent>,
    IConsumer<DomainEvents.UserInviteAccepted>,
    IConsumer<DomainEvents.UserInviteRevoked>,
    IConsumer<DomainEvents.UserInviteExpired>
{
    public Task Consume(ConsumeContext<DomainEvents.UserInviteCreated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserInviteResent> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserInviteAccepted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserInviteRevoked> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserInviteExpired> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}
