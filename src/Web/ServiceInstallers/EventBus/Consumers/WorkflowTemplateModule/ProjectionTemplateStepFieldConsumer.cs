using MassTransit;
using WorkflowTemplate.Application.UseCases.Events;
using WorkflowTemplate.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowTemplateModule;

public class ProjectionTemplateStepFieldConsumer(IProjectionTemplateStepFieldEventHandler eventHandler) :
    IConsumer<DomainEvents.WorkflowTemplateStepFieldAdded>,
    IConsumer<DomainEvents.WorkflowTemplateStepFieldUpdated>,
    IConsumer<DomainEvents.WorkflowTemplateStepFieldRemoved>
{
    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepFieldAdded> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepFieldUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepFieldRemoved> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}