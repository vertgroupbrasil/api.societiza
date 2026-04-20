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

        // InvitationLink events
        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer,
            IdentityDomainEvent.InvitationLinkCreated>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer,
            IdentityDomainEvent.InvitationLinkConsumed>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer,
            IdentityDomainEvent.InvitationLinkExhausted>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer,
            IdentityDomainEvent.InvitationLinkExpired>(registrationContext, MODULE_NAME);

        rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionInvitationLinkConsumer,
            IdentityDomainEvent.InvitationLinkRevoked>(registrationContext, MODULE_NAME);
    }
}