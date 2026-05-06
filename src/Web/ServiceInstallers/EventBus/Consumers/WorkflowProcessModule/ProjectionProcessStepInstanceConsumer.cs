using MassTransit;
using WorkflowProcess.Application.UseCases.Events.ProcessStep;
using WorkflowProcess.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowProcessModule;

public class ProjectionProcessStepInstanceConsumer(IProjectionProcessStepInstanceEventHandler eventHandler) :
    IConsumer<DomainEvents.ProcessStepInstanceMaterialized>,
    IConsumer<DomainEvents.ProcessStepInstanceStarted>,
    IConsumer<DomainEvents.ProcessStepInstanceCompleted>,
    IConsumer<DomainEvents.ProcessStepInstanceReverted>
{
    public Task Consume(ConsumeContext<DomainEvents.ProcessStepInstanceMaterialized> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.ProcessStepInstanceStarted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.ProcessStepInstanceCompleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.ProcessStepInstanceReverted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}