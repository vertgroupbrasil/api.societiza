# Tarefa 4: Projeções de Autenticação

> Parte 4 da estruturação da Tarefa 4 da Fase 1 do Roadmap Societiza.
> Cobre a extensão da camada de leitura para suportar os novos aggregates de autenticação: projection models, event handlers e consumers.
> Gerado em 01/04/2026.

---

## Visão Geral

Este documento estende a camada de projeção do módulo Identity para suportar os eventos introduzidos no Documento 3. Ao final desta parte, os eventos de refresh token e password reset token estarão sendo consumidos do RabbitMQ e projetados no MongoDB, e o `ProjectionModel.User` estará reagindo ao novo evento `UserPasswordUpdated`.

**Dependências:** este documento pressupõe que os eventos de domínio e os métodos do aggregate `User` definidos no Documento 3 já estão implementados.

---

## Estrutura de Arquivos

```
Identity.Domain/
└── Projections/
    └── ProjectionModel.cs              ← adicionar RefreshToken e PasswordResetToken

Identity.Application/
└── UseCases/
    └── Events/
        ├── ProjectionUserEventHandler.cs             ← atualizar: adicionar UserPasswordUpdated
        ├── ProjectionRefreshTokenEventHandler.cs     ← novo
        └── ProjectionPasswordResetTokenEventHandler.cs ← novo

Web/
└── ServiceInstallers/
    └── EventBus/
        └── Consumers/
            └── IdentityModule/
                ├── ProjectionUserConsumer.cs             ← atualizar: adicionar UserPasswordUpdated
                ├── ProjectionRefreshTokenConsumer.cs     ← novo
                ├── ProjectionPasswordResetTokenConsumer.cs ← novo
                └── ConsumerConfiguration.cs             ← atualizar: registrar novos consumers
```

---

## 1. ProjectionModel — Extensão

**Localização:** `Identity.Domain/Projections/ProjectionModel.cs`

Adicionar os novos projection models ao arquivo existente. `ProjectionModel.User` permanece sem alterações — tokens têm projeções próprias e coleções MongoDB independentes.

```csharp
public record RefreshToken(
    Guid Id,
    Guid UserId,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    bool IsRevoked,
    DateTimeOffset? RevokedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsDeleted = false
) : IProjectionModel;

public record PasswordResetToken(
    Guid Id,
    Guid UserId,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    bool IsUsed,
    DateTimeOffset? UsedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsDeleted = false
) : IProjectionModel;
```

---

## 2. Event Handlers

### ProjectionUserEventHandler — Atualização

**Localização:** `Identity.Application/UseCases/Events/ProjectionUserEventHandler.cs`

Substituir o arquivo existente — adiciona `UserPasswordUpdated` à interface e à implementação:

```csharp
using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionUserEventHandler :
    IEventHandler<DomainEvents.UserRegistrationCompleted>,
    IEventHandler<DomainEvents.UserPasswordUpdated>;

public class ProjectionUserEventHandler(
    IIdentityProjection<ProjectionModel.User> userProjection,
    ILogger<ProjectionUserEventHandler> logger
) : IProjectionUserEventHandler
{
    public async Task Handle(DomainEvents.UserRegistrationCompleted @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await userProjection.ReplaceInsertAsync(new(
                @event.Id,
                @event.FirstName,
                @event.LastName,
                @event.Email,
                @event.Role,
                @event.AccountancyId,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project user registration: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project user registration: {@event.Id}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserPasswordUpdated @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await userProjection.UpdateFieldsAsync(@event.Id, builder => builder
                .Set(x => x.UpdatedAt, @event.UpdatedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project password updated for user: {Id}.", @event.Id);
            throw new InvalidOperationException($"Failed to project password updated for user: {@event.Id}.", ex);
        }
    }
}
```

> `UserPasswordUpdated` atualiza apenas `UpdatedAt` na projeção — o `PasswordHash` nunca é armazenado no MongoDB.

---

### ProjectionRefreshTokenEventHandler — Novo

**Localização:** `Identity.Application/UseCases/Events/ProjectionRefreshTokenEventHandler.cs`

```csharp
using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionRefreshTokenEventHandler :
    IEventHandler<DomainEvents.UserRefreshTokenAdded>,
    IEventHandler<DomainEvents.UserRefreshTokenRevoked>;

public class ProjectionRefreshTokenEventHandler(
    IIdentityProjection<ProjectionModel.RefreshToken> refreshTokenProjection,
    ILogger<ProjectionRefreshTokenEventHandler> logger
) : IProjectionRefreshTokenEventHandler
{
    public async Task Handle(DomainEvents.UserRefreshTokenAdded @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await refreshTokenProjection.ReplaceInsertAsync(new(
                @event.TokenId,
                @event.UserId,
                @event.TokenHash,
                @event.ExpiresAt,
                IsRevoked: false,
                RevokedAt: null,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project refresh token added: {Id}.", @event.TokenId);
            throw new InvalidOperationException($"Failed to project refresh token added: {@event.TokenId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserRefreshTokenRevoked @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await refreshTokenProjection.UpdateFieldsAsync(@event.TokenId, builder => builder
                .Set(x => x.IsRevoked, true)
                .Set(x => x.RevokedAt, @event.RevokedAt)
                .Set(x => x.UpdatedAt, @event.RevokedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project refresh token revoked: {Id}.", @event.TokenId);
            throw new InvalidOperationException($"Failed to project refresh token revoked: {@event.TokenId}.", ex);
        }
    }
}
```

---

### ProjectionPasswordResetTokenEventHandler — Novo

**Localização:** `Identity.Application/UseCases/Events/ProjectionPasswordResetTokenEventHandler.cs`

```csharp
using Core.Application.EventBus;
using Identity.Domain.Events;
using Identity.Domain.Projection;
using Identity.Persistence.Projection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.UseCases.Events;

public interface IProjectionPasswordResetTokenEventHandler :
    IEventHandler<DomainEvents.UserPasswordResetTokenAdded>,
    IEventHandler<DomainEvents.UserPasswordResetTokenUsed>;

public class ProjectionPasswordResetTokenEventHandler(
    IIdentityProjection<ProjectionModel.PasswordResetToken> passwordResetTokenProjection,
    ILogger<ProjectionPasswordResetTokenEventHandler> logger
) : IProjectionPasswordResetTokenEventHandler
{
    public async Task Handle(DomainEvents.UserPasswordResetTokenAdded @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await passwordResetTokenProjection.ReplaceInsertAsync(new(
                @event.TokenId,
                @event.UserId,
                @event.TokenHash,
                @event.ExpiresAt,
                IsUsed: false,
                UsedAt: null,
                @event.CreatedAt,
                null),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project password reset token added: {Id}.", @event.TokenId);
            throw new InvalidOperationException($"Failed to project password reset token added: {@event.TokenId}.", ex);
        }
    }

    public async Task Handle(DomainEvents.UserPasswordResetTokenUsed @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await passwordResetTokenProjection.UpdateFieldsAsync(@event.TokenId, builder => builder
                .Set(x => x.IsUsed, true)
                .Set(x => x.UsedAt, @event.UsedAt)
                .Set(x => x.UpdatedAt, @event.UsedAt),
            cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to project password reset token used: {Id}.", @event.TokenId);
            throw new InvalidOperationException($"Failed to project password reset token used: {@event.TokenId}.", ex);
        }
    }
}
```

---

## 3. Consumers

### ProjectionUserConsumer — Atualização

**Localização:** `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionUserConsumer.cs`

Substituir o consumer existente — `UserPasswordUpdated` adicionado:

```csharp
using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionUserConsumer(IProjectionUserEventHandler eventHandler) :
    IConsumer<DomainEvents.UserRegistrationCompleted>,
    IConsumer<DomainEvents.UserPasswordUpdated>
{
    public Task Consume(ConsumeContext<DomainEvents.UserRegistrationCompleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserPasswordUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}
```

---

### ProjectionRefreshTokenConsumer — Novo

**Localização:** `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionRefreshTokenConsumer.cs`

```csharp
using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionRefreshTokenConsumer(IProjectionRefreshTokenEventHandler eventHandler) :
    IConsumer<DomainEvents.UserRefreshTokenAdded>,
    IConsumer<DomainEvents.UserRefreshTokenRevoked>
{
    public Task Consume(ConsumeContext<DomainEvents.UserRefreshTokenAdded> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserRefreshTokenRevoked> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}
```

---

### ProjectionPasswordResetTokenConsumer — Novo

**Localização:** `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ProjectionPasswordResetTokenConsumer.cs`

```csharp
using Identity.Application.UseCases.Events;
using Identity.Domain.Events;
using MassTransit;

namespace Web.ServiceInstallers.EventBus.Consumers.IdentityModule;

public class ProjectionPasswordResetTokenConsumer(IProjectionPasswordResetTokenEventHandler eventHandler) :
    IConsumer<DomainEvents.UserPasswordResetTokenAdded>,
    IConsumer<DomainEvents.UserPasswordResetTokenUsed>
{
    public Task Consume(ConsumeContext<DomainEvents.UserPasswordResetTokenAdded> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserPasswordResetTokenUsed> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}
```

---

### ConsumerConfiguration — Atualização

**Localização:** `src/Web/ServiceInstallers/EventBus/Consumers/IdentityModule/ConsumerConfiguration.cs`

Substituir o arquivo existente:

```csharp
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
```

---

*Gerado em 01/04/2026.*