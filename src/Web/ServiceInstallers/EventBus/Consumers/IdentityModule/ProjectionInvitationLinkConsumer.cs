using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionInvitationLinkConsumer(IProjectionInvitationLinkEventHandler eventHandler) :
    IConsumer<DomainEvents.InvitationLinkCreated>,
    IConsumer<DomainEvents.InvitationLinkConsumed>,
    IConsumer<DomainEvents.InvitationLinkExhausted>,
    IConsumer<DomainEvents.InvitationLinkExpired>,
    IConsumer<DomainEvents.InvitationLinkRevoked>
{
    public Task Consume(ConsumeContext<DomainEvents.InvitationLinkCreated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.InvitationLinkConsumed> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.InvitationLinkExhausted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.InvitationLinkExpired> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.InvitationLinkRevoked> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}