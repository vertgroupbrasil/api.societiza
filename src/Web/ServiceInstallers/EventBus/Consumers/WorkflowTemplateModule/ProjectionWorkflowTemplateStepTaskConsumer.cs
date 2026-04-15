using MassTransit;
using WorkflowTemplate.Application.UseCases.Events.TemplateStepTask;
using WorkflowTemplate.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowTemplateModule;

public class ProjectionWorkflowTemplateStepTaskConsumer(IProjectionWorkflowTemplateStepTaskEventHandler eventHandler) :
    IConsumer<DomainEvents.WorkflowTemplateStepTaskAdded>,
    IConsumer<DomainEvents.WorkflowTemplateStepTaskUpdated>,
    IConsumer<DomainEvents.WorkflowTemplateStepTaskRemoved>
{
    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepTaskAdded> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepTaskUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepTaskRemoved> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}