using MassTransit;
using WorkflowProcess.Application.UseCases.Events.Process;
using WorkflowProcess.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowProcessModule;

public class ProjectionWorkflowProcessConsumer(IProjectionWorkflowProcessEventHandler eventHandler) :
    IConsumer<DomainEvents.WorkflowProcessCreated>,
    IConsumer<DomainEvents.WorkflowProcessCompleted>,
    IConsumer<DomainEvents.WorkflowProcessReverted>
{
    public Task Consume(ConsumeContext<DomainEvents.WorkflowProcessCreated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowProcessCompleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.WorkflowProcessReverted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}