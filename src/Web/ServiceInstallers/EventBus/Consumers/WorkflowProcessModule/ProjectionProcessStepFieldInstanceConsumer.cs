using MassTransit;
using WorkflowProcess.Application.UseCases.Events.ProcessStep;
using WorkflowProcess.Domain.Events;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowProcessModule;

public class ProjectionProcessStepFieldInstanceConsumer(IProjectionProcessStepFieldInstanceEventHandler eventHandler) :
    IConsumer<DomainEvents.ProcessStepFieldInstanceMaterialized>,
    IConsumer<DomainEvents.ProcessStepFieldInstanceFilled>
{
    public Task Consume(ConsumeContext<DomainEvents.ProcessStepFieldInstanceMaterialized> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.ProcessStepFieldInstanceFilled> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}