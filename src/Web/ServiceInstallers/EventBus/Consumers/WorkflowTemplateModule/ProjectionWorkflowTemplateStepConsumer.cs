using MassTransit;
using WorkflowTemplate.Application.UseCases.Events.TemplateStep;
using WorkflowTemplate.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowTemplateModule;

public class ProjectionWorkflowTemplateStepConsumer(IProjectionWorkflowTemplateStepEventHandler eventHandler) :
    IConsumer<DomainEvents.WorkflowTemplateStepAdded>,
    IConsumer<DomainEvents.WorkflowTemplateStepUpdated>,
    IConsumer<DomainEvents.WorkflowTemplateStepRemoved>
{
    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepAdded> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowTemplateStepRemoved> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}