using MassTransit;
using WorkflowProcess.Application.UseCases.Events.ProcessStepTask;
using WorkflowProcess.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowProcessModule;

public class ProjectionProcessStepTaskFieldInstanceConsumer(IProjectionProcessStepTaskFieldInstanceEventHandler eventHandler) :
    IConsumer<DomainEvents.ProcessStepTaskFieldInstanceMaterialized>,
    IConsumer<DomainEvents.ProcessStepTaskFieldInstanceFilled>
{
    public Task Consume(ConsumeContext<DomainEvents.ProcessStepTaskFieldInstanceMaterialized> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.ProcessStepTaskFieldInstanceFilled> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}