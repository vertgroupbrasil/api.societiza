using MassTransit;
using WorkflowTemplate.Application.UseCases.Events.TemplateStepTask;
using WorkflowTemplate.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowTemplateModule;

public class ProjectionTemplateStepTaskFieldConsumer(IProjectionTemplateStepTaskFieldEventHandler eventHandler) :
    IConsumer<DomainEvents.WorkflowTemplateStepTaskFieldAdded>,
    IConsumer<DomainEvents.WorkflowTemplateStepTaskFieldUpdated>,
    IConsumer<DomainEvents.WorkflowTemplateStepTaskFieldRemoved>
{
    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepTaskFieldAdded> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepTaskFieldUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepTaskFieldRemoved> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}