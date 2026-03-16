using MassTransit;
using WorkflowTemplate.Application.UseCases.Events;
using WorkflowTemplate.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowTemplateModule;

public class ProjectionWorkflowTemplateCascadeDeleteConsumer(
    IProjectionWorkflowTemplateCascadeDeleteEventHandler eventHandler) :
    IConsumer<DomainEvents.WorkflowTemplateStepCascadeDeleted>,
    IConsumer<DomainEvents.WorkflowTemplateStepTaskCascadeDeleted>
{
    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepCascadeDeleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepTaskCascadeDeleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}