using Core.Infrastructure.EventBus.Configurations;
using MassTransit;
using Web.ServiceInstallers.EventBus.Extensions;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

using IdentityDomainEvent = Identity.Domain.Events.DomainEvents;

internal sealed class ConsumerConfiguration : IEventReceiveEndpointConfiguration
{
    private const string MODULE_NAME = "identity";

    public void AddEventReceiveEndpoints(
        IRabbitMqBusFactoryConfigurator rabbitMqBusFactoryConfigurator,
        IRegistrationContext registrationContext)
    {
        // User events
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserConsumer,
            IdentityDomainEvent.UserRegistrationCompleted>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserConsumer,
            IdentityDomainEvent.UserPasswordUpdated>(registrationContext, MODULE_NAME);

        // RefreshToken events
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionRefreshTokenConsumer,
            IdentityDomainEvent.UserRefreshTokenAdded>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionRefreshTokenConsumer,
            IdentityDomainEvent.UserRefreshTokenRevoked>(registrationContext, MODULE_NAME);

        // PasswordResetToken events
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionPasswordResetTokenConsumer,
            IdentityDomainEvent.UserPasswordResetTokenAdded>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionPasswordResetTokenConsumer,
            IdentityDomainEvent.UserPasswordResetTokenUsed>(registrationContext, MODULE_NAME);

        // UserInvite events
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserInviteConsumer,
            IdentityDomainEvent.UserInviteCreated>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserInviteConsumer,
            IdentityDomainEvent.UserInviteResent>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserInviteConsumer,
            IdentityDomainEvent.UserInviteAccepted>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserInviteConsumer,
            IdentityDomainEvent.UserInviteRevoked>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserInviteConsumer,
            IdentityDomainEvent.UserInviteExpired>(registrationContext, MODULE_NAME);
    }
}
