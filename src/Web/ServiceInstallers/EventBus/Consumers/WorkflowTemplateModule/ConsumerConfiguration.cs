using Core.Infrastructure.EventBus.Configurations;
using MassTransit;
using Web.ServiceInstallers.EventBus.Extensions;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowTemplateModule;

using WorkflowDomainEvent = WorkflowTemplate.Domain.Events.DomainEvents;

internal sealed class ConsumerConfiguration : IEventReceiveEndpointConfiguration
{
    private const string MODULE_NAME = "workflow-template";

    public void AddEventReceiveEndpoints(IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator, IRegistrationContext registrationContext)
    {
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateConsumer, WorkflowDomainEvent.WorkflowTemplateCreated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateConsumer, WorkflowDomainEvent.WorkflowTemplateCreated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateConsumer, WorkflowDomainEvent.WorkflowTemplateActivated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateConsumer, WorkflowDomainEvent.WorkflowTemplateArchived>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepConsumer, WorkflowDomainEvent.WorkflowTemplateStepAdded>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepConsumer, WorkflowDomainEvent.WorkflowTemplateStepUpdated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepConsumer, WorkflowDomainEvent.WorkflowTemplateStepRemoved>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepTaskConsumer, WorkflowDomainEvent.WorkflowTemplateStepTaskAdded>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepTaskConsumer, WorkflowDomainEvent.WorkflowTemplateStepTaskUpdated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepTaskConsumer, WorkflowDomainEvent.WorkflowTemplateStepTaskRemoved>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionTemplateStepFieldConsumer, WorkflowDomainEvent.WorkflowTemplateStepFieldAdded>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionTemplateStepFieldConsumer, WorkflowDomainEvent.WorkflowTemplateStepFieldUpdated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionTemplateStepFieldConsumer, WorkflowDomainEvent.WorkflowTemplateStepFieldRemoved>(registrationContext, MODULE_NAME);
    }
}