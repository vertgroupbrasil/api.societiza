using Core.Infrastructure.EventBus.Configurations;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.WorkflowProcessModule;

internal sealed class ConsumerConfiguration : IEventReceiveEndpointConfiguration
{
    private const string MODULE_NAME = "workflow-process";

    public void AddEventReceiveEndpoints(IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator, IRegistrationContext registrationContext)
    {
        throw new NotImplementedException();
    }
}