using Core.Infrastructure.EventBus.Configurations;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.UserModule;

internal sealed class ConsumerConfiguration : IEventReceiveEndpointConfiguration
{
    private const string MODULE_NAME = "user";
    public void AddEventReceiveEndpoints(IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator, IRegistrationContext registrationContext)
    {
        throw new NotImplementedException();
    }
}