using Core.Infrastructure.EventBus.Configurations;
using MassTransit;
using Web.ServiceInstallers.EventBus.Extensions;

namespace Web.ServiceInstallers.EventBus.Consumers.AccountancyModule;

using AccountancyDomainEvent = Accountancy.Domain.Events.DomainEvents;

internal sealed class ConsumerConfiguration : IEventReceiveEndpointConfiguration
{
    private const string MODULE_NAME = "accountancy";

    public void AddEventReceiveEndpoints(IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator, IRegistrationContext registrationContext)
    {
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionAccountancyConsumer, AccountancyDomainEvent.AccountancyCreated>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionAccountancyConsumer, AccountancyDomainEvent.AccountancyUpdated>(registrationContext, MODULE_NAME);
    }
}