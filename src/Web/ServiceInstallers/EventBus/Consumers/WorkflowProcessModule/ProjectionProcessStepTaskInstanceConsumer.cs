using MassTransit;
using WorkflowProcess.Application.UseCases.Events.ProcessStepTask;
using WorkflowProcess.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowProcessModule;

public class ProjectionProcessStepTaskInstanceConsumer(IProjectionProcessStepTaskInstanceEventHandler eventHandler) :
    IConsumer<DomainEvents.ProcessStepTaskInstanceMaterialized>,
    IConsumer<DomainEvents.ProcessStepTaskInstanceCompleted>,
    IConsumer<DomainEvents.ProcessStepTaskInstanceSkipped>,
    IConsumer<DomainEvents.ProcessStepTaskInstanceReverted>
{
    public Task Consume(ConsumeContext<DomainEvents.ProcessStepTaskInstanceMaterialized> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.ProcessStepTaskInstanceCompleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.ProcessStepTaskInstanceSkipped> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.ProcessStepTaskInstanceReverted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}