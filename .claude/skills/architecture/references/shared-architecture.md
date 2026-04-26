# Camada: Shared

> A camada Shared expõe tudo que outras partes do sistema precisam deste módulo — comandos e DTOs de resposta — sem criar dependências circulares.
> Depende apenas de Core. Nunca referencia as camadas Domain ou Application do módulo.

---

## Estrutura

```
{Module}.Shared/
├── Commands/        → ICommand and ICommand<TResponse> definitions
├── Queries/         → IQuery<TResponse> definitions (if the module has queries)
├── Emails/          → email delivery commands
└── Response/        → sealed record Response DTOs
```

---

## Comandos

Todos os comandos são `sealed record` que implementam `ICommand` ou `ICommand<TResponse>`.

### Comando sem resposta

Use para mutações que não retornam dados:

```csharp
// src/Modules/Identity/Identity.Shared/Commands/RevokeInviteCommand.cs
namespace Identity.Shared.Commands;

public sealed record RevokeInviteCommand(Guid InviteId) : ICommand;
```

### Comando com resposta

Use quando o comando cria um recurso e o chamador precisa do Id:

```csharp
// src/Modules/Identity/Identity.Shared/Commands/InviteUserCommand.cs
namespace Identity.Shared.Commands;

public sealed record InviteUserCommand(
    string Email,
    string Role,
    Guid? AccountancyId
) : ICommand<IdentifierResponse>;
```

`IdentifierResponse` é o tipo de retorno padrão para criação de recurso — definido em `Core.Shared`.

---

## Comandos de Email

Vivem em `{Module}.Shared/Emails/` — não em `Commands/` — porque são comandos de entrega, não comandos de negócio.

```csharp
// src/Modules/Identity/Identity.Shared/Emails/SendInviteEmailCommand.cs
namespace Identity.Shared.Emails;

public sealed record SendInviteEmailCommand(
    string ToEmail,
    string ToName,
    string InviteLink
) : ICommand;
```

**Todos os campos devem ser tipos primitivos (`string`, `int`, `Guid`, `bool`).**
O Hangfire precisa serializar e desserializar o comando — sem objetos de domínio, sem SmartEnums, sem tipos complexos.

---

## DTOs de Resposta

DTOs de resposta são retornados por handlers de Query e expostos por endpoints. Eles vivem em `{Module}.Shared/Response/`.

```csharp
// src/Modules/Accountancy/Accountancy.Shared/Response/AccountancyDetailResponse.cs
namespace Accountancy.Shared.Response;

public sealed record AccountancyDetailResponse(
    Guid Id,
    string CNPJ,
    string LegalName,
    string? TradeName,
    string Address,
    string City,
    string State,
    string PostalCode,
    string Phone,
    string? Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
```

**Regras de resposta:**
- `sealed record` — sem herança, sem interfaces
- Todos os campos são primitivos, `Guid`, `DateTimeOffset` ou variantes nullable — nunca tipos de Domain ou SmartEnums
- Nomeados conforme o recurso e a intenção: `{Resource}DetailResponse`, `{Resource}ListResponse`, `{Resource}SummaryResponse`
- Definidos em `{Module}.Shared/Response/` — nunca em Application ou Web

---

## O que NUNCA fazer em Shared

- Nunca referencie as camadas Domain ou Application do módulo a partir de Shared.
- Nunca use SmartEnum, entities ou value objects como campos de comando ou resposta — apenas primitivos.
- Nunca defina DTOs de resposta em Application ou Web — eles sempre pertencem a Shared.
- Nunca coloque comandos de email em `Commands/` — eles pertencem a `Emails/`.
- Nunca use tipos não primitivos em campos de comandos de email — a serialização do Hangfire vai quebrar.