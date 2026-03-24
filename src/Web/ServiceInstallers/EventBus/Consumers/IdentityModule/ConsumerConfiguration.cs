using Core.Infrastructure.EventBus.Configurations;
using MassTransit;
using Web.ServiceInstallers.EventBus.Extensions;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

using IdentityDomainEvent = Identity.Domain.Events.DomainEvents;

internal sealed class ConsumerConfiguration : IEventReceiveEndpointConfiguration
{
    private const string MODULE_NAME = "identity";

    public void AddEventReceiveEndpoints(IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator, IRegistrationContext registrationContext)
    {
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserConsumer, IdentityDomainEvent.UserInvited>(registrationContext, MODULE_NAME);
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserConsumer, IdentityDomainEvent.UserRegistrationCompleted>(registrationContext, MODULE_NAME);
    }
}