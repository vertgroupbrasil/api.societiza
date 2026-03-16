using Core.Infrastructure.EventBus.Configurations;
using MassTransit;
using Web.ServiceInstallers.EventBus.Extensions;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowTemplateModule;

using Core.Infrastructure.EventBus.Configurations;
using MassTransit;
using WorkflowTemplateDomainEvent = WorkflowTemplate.Domain.Events.DomainEvents;

internal sealed class ConsumerConfiguration : IEventReceiveEndpointConfiguration
{
    private const string MODULE_NAME = "workflow-template";

    public void AddEventReceiveEndpoints(IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator, IRegistrationContext registrationContext)
    {
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateCreated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateUpdated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateActivated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateArchived>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepAdded>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepUpdated>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepTaskConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepTaskAdded>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateStepTaskConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepTaskUpdated>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionTemplateStepFieldConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepFieldAdded>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionTemplateStepFieldConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepFieldUpdated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionTemplateStepFieldConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepFieldRemoved>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionTemplateStepTaskFieldConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepTaskFieldAdded>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionTemplateStepTaskFieldConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepTaskFieldUpdated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionTemplateStepTaskFieldConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepTaskFieldRemoved>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateCascadeDeleteConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepCascadeDeleted>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionWorkflowTemplateCascadeDeleteConsumer, WorkflowTemplateDomainEvent.WorkflowTemplateStepTaskCascadeDeleted>(registrationContext, MODULE_NAME);
    }
}