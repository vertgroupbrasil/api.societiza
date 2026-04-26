# Consumers Web

> Web é dona das classes consumer MassTransit e da configuração dos receive endpoints RabbitMQ.
> Consumers são adapters. Eles recebem mensagens de integração e delegam para handlers de Application. Eles não contêm lógica de projection nem lógica de negócio.

---

## Layout de Arquivos

```text
src/Web/ServiceInstallers/EventBus/
+-- EventBusServiceInstaller.cs
+-- Extensions/RabbitMqBusFactoryConfiguratorExtensions.cs
+-- Consumers/{Module}Module/
    +-- ConsumerConfiguration.cs
    +-- Projection{Aggregate}Consumer.cs
```

---

## EventBusServiceInstaller

`EventBusServiceInstaller` configura MassTransit e RabbitMQ.

Comportamento existente importante:

- Usa `SetKebabCaseEndpointNameFormatter`.
- Descobre consumers com `AddConsumers(AssemblyReference.Assembly)`.
- Adiciona clients de requisição a partir dos assemblies.
- Adiciona consumers do Hangfire.
- Usa redelivery atrasado e retries imediatos.
- Usa Newtonsoft JSON mais converters do projeto.
- Aplica filtros/observers de tracing e logging.
- Descobre configurações de receive endpoint a partir dos assemblies.

Não registre consumers manualmente em DI quando a descoberta por assembly já os
cobre.

---

## Classes Consumer

Use um projection consumer por aggregate ou grupo de projection.

Formato de exemplo:

```csharp
public class ProjectionUserConsumer(IProjectionUserEventHandler eventHandler) :
    IConsumer<DomainEvents.UserRegistrationCompleted>,
    IConsumer<DomainEvents.UserPasswordUpdated>,
    IConsumer<DomainEvents.UserNameUpdated>
{
    public Task Consume(ConsumeContext<DomainEvents.UserRegistrationCompleted> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserPasswordUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<DomainEvents.UserNameUpdated> context)
        => eventHandler.Handle(context.Message, context.CancellationToken);
}
```

Regras:

- Métodos consumer delegam imediatamente.
- Use `context.Message`.
- Use `context.CancellationToken`.
- Mantenha lógica de projection em Application.
- Mantenha detalhes de persistence fora de Web.

---

## ConsumerConfiguration

Cada módulo tem um `ConsumerConfiguration`.

Ele implementa:

```csharp
IEventReceiveEndpointConfiguration
```

Registre explicitamente cada par `<consumer, event>`:

```csharp
rabbitMqBusFactoryConfigurator.ConfigureEventReceiveEndpoint<ProjectionUserConsumer,
    IdentityDomainEvent.UserRegistrationCompleted>(registrationContext, MODULE_NAME);
```

Regras:

- Use `private const string MODULE_NAME = "{module}"`.
- Adicione uma chamada por evento.
- Mantenha eventos agrupados por aggregate ou tópico de projection.
- Não dependa apenas de `AddConsumers`; sem configuração de receive endpoint, a fila dedicada não existirá.

---

## Nomenclatura de Filas

Nomes de fila são gerados por:

```text
src/Web/ServiceInstallers/EventBus/Extensions/RabbitMqBusFactoryConfiguratorExtensions.cs
```

Formato:

```text
webbff.{module}.{consumer-kebab-case}.{event-kebab-case}
```

Exemplo:

```text
webbff.identity.projection-user-consumer.user-registration-completed
```

---

## Nunca Fazer

- Não coloque lógica de negócio em um consumer.
- Não coloque lógica de atualização de projection em Web.
- Não esqueça entradas de `ConsumerConfiguration` para novos eventos.
- Não crie nomes de fila ad hoc.
- Não contorne a extension `ConfigureEventReceiveEndpoint` existente.

---