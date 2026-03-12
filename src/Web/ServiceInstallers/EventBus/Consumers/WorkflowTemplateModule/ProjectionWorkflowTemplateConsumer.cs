using MassTransit;
using WorkflowTemplate.Application.UseCases.Events;
using WorkflowTemplate.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowTemplateModule;

public class ProjectionWorkflowTemplateConsumer(IProjectionWorkflowTemplateEventHandler eventHandler) :
    IConsumer<DomainEvents.WorkflowTemplateCreated>,
    IConsumer<DomainEvents.WorkflowTemplateUpdated>,
    IConsumer<DomainEvents.WorkflowTemplateActivated>,
    IConsumer<DomainEvents.WorkflowTemplateArchived>
{
    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateCreated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateActivated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateArchived> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}