using Accountancy.Application.UseCases.Events;
using Accountancy.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.AccountancyModule;

public class ProjectionAccountancyConsumer(IProjectionAccountancyEventHandler eventHandler) :
    IConsumer<DomainEvents.AccountancyCreated>,
    IConsumer<DomainEvents.AccountancyUpdated>,
    IConsumer<DomainEvents.AccountancyDeleted>
{
    public Task Consume(ConsumeContext<DomainEvents.AccountancyCreated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.AccountancyUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.AccountancyDeleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}