using Core.Infrastructure.EventBus.Configurations;
using MassTransit;
using Web.ServiceInstallers.EventBus.Extensions;
using WorkflowProcessDomainEvent = WorkflowProcess.Domain.Events.DomainEvents;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowProcessModule;

internal sealed class ConsumerConfiguration : IEventReceiveEndpointConfiguration
{
    private const string MODULE_NAME = "workflow-process";

    public void AddEventReceiveEndpoints(IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator, IRegistrationContext registrationContext)
    {
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowProcessConsumer, WorkflowProcessDomainEvent.WorkflowProcessCreated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowProcessConsumer, WorkflowProcessDomainEvent.WorkflowProcessCompleted>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowProcessConsumer, WorkflowProcessDomainEvent.WorkflowProcessReverted>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepInstanceMaterialized>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepInstanceStarted>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepInstanceCompleted>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepInstanceReverted>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepTaskInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepTaskInstanceMaterialized>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepTaskInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepTaskInstanceCompleted>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepTaskInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepTaskInstanceSkipped>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepTaskInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepTaskInstanceReverted>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepFieldInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepFieldInstanceMaterialized>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepFieldInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepFieldInstanceFilled>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepTaskFieldInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepTaskFieldInstanceMaterialized>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionProcessStepTaskFieldInstanceConsumer, WorkflowProcessDomainEvent.ProcessStepTaskFieldInstanceFilled>(registrationContext, MODULE_NAME);
    }
}