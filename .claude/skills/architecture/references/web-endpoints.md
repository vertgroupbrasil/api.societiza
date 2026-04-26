# Endpoints Web

> A camada Web é o host ASP.NET Core. Ela é dona dos endpoints HTTP, DTOs de requisição, rotas, startup tasks e wiring do MassTransit.
> Endpoints devem permanecer finos. Eles traduzem entrada HTTP em comandos ou queries e delegam imediatamente para Application por meio do MediatR.

---

## Fronteira de Responsabilidade

Web é dona de:

- Endpoints
- DTOs de requisição
- Constantes de rota
- Declarações de status HTTP
- Atributos de autenticação e autorização
- Atributos Swagger, com regras de wording de `web-swagger.md`

Web não é dona de:

- Lógica de negócio
- Decisões de Domain
- Regras de Persistence
- Lógica de projection
- Composição de corpo de email

---

## Layout de Arquivos

```text
src/Web/Endpoints/
+-- Tags.cs
+-- Routes/
|   +-- {Module}Routes.cs
+-- Modules/{Module}/
    +-- {Operation}Endpoint.cs
    +-- Requests/
        +-- Commands/{Operation}Request.cs
        +-- Queries/{Operation}Request.cs
+-- Modules/{Module}/{Feature}/
    +-- {Operation}Endpoint.cs
    +-- Requests/
        +-- Commands/{Operation}Request.cs
        +-- Queries/{Operation}Request.cs
```

Use as pastas de funcionalidade existentes:

- `Accountancy`
- `Identity/Auth`
- `Identity/Invitation`
- `Identity/User`
- `WorkflowTemplate/Template`
- `WorkflowTemplate/TemplateStep`
- `WorkflowTemplate/TemplateStepTask`

---

## Formato do Endpoint

Todos os endpoints herdam de `EndpointBaseAsync`.

```csharp
// With request and response body
EndpointBaseAsync
    .WithRequest<TRequest>
    .WithActionResult<TResponse>

// With request and no response body
EndpointBaseAsync
    .WithRequest<TRequest>
    .WithActionResult

// Without request and with response body
EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<TResponse>

// Without request and no response body
EndpointBaseAsync
    .WithoutRequest
    .WithActionResult
```

Classes de endpoint são sempre `public sealed`.

Nomeie endpoints como `{Operation}Endpoint`.

`OperationId` no Swagger deve ser o nome da classe sem o sufixo `Endpoint`.

---

## Endpoint Canônico

O texto de Swagger está abreviado aqui de propósito. Use `web-swagger.md` para o
formato completo de documentação.

```csharp
[Authorize(Policy = IdentityPolicies.CanManageUsers)]
public sealed class DeleteUserEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<DeleteUserRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpDelete(IdentityRoutes.DeleteUser)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "DeleteUser",
        Summary     = "Excluir usuário",
        Description = """...""",
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult> HandleAsync(
        DeleteUserRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new DeleteUserCommand(r.UserId, r.AccountancyId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
```

---

## Decorators Obrigatórios

Coloque estes atributos em `HandleAsync`:

- `[ApiVersion("1.0")]`
- `[HttpGet|HttpPost|HttpPut|HttpPatch|HttpDelete({Module}Routes.X)]`
- `[ProducesResponseType(...)]` para todo status possível
- `[SwaggerOperation(...)]`

Coloque `[Authorize]` na classe:

- Use `[Authorize]` para qualquer usuário autenticado.
- Use `[Authorize(Policy = IdentityPolicies.X)]` para endpoints protegidos por policy.
- Não coloque `[Authorize]` em `HandleAsync`.
- Endpoints públicos intencionalmente não têm `[Authorize]`.

---

## Pipeline Railway

Todo endpoint deve seguir o mesmo fluxo:

```csharp
await Result.Create(request)
    .Map(r => new XCommand(...))
    .Bind(command => sender.Send(command, cancellationToken))
    .Match(Ok, this.HandleFailure);
```

Use estes handlers de sucesso:

- Com corpo de resposta: `.Match(Ok, this.HandleFailure)`
- Sem corpo de resposta: `.Match(() => Ok(), this.HandleFailure)`

Use `.Map(...)` apenas para traduzir dados da requisição em um comando ou query.

Use `.Bind(...)` para chamadas MediatR que retornam `Result`.

---

## DTOs de Requisição

DTOs de requisição vivem em:

```text
src/Web/Endpoints/Modules/{Module}/Requests/{Commands|Queries}/
src/Web/Endpoints/Modules/{Module}/{Feature}/Requests/{Commands|Queries}/
```

Regras:

- Use `sealed class`.
- Use propriedades públicas settable com defaults seguros.
- Use `[FromRoute(Name = Routes.Constant)]` para valores de rota.
- Use `[FromQuery]` para valores de query.
- Não adicione `[FromBody]` a campos normais de corpo em POST ou PUT.
- Não passe DTOs de requisição para handlers.
- Sempre mapeie DTOs de requisição para comandos ou queries no endpoint.

Exemplo:

```csharp
public sealed class DeleteUserRequest
{
    [FromRoute(Name = IdentityRoutes.UserId)]
    public Guid UserId { get; set; }

    [FromQuery]
    public Guid? AccountancyId { get; set; }
}
```

---

## Rotas

Rotas vivem em:

```text
src/Web/Endpoints/Routes/{Module}Routes.cs
```

Regras:

- Use uma `internal static class` por módulo.
- `BaseUri` inclui `v{version:apiVersion}/{module}`.
- Não inclua `/api`; o host adiciona.
- Nomes de parâmetros de rota são constantes.
- Parâmetros de rota usam constraints, geralmente `:guid`.
- Reutilize a mesma constante em templates de rota e `[FromRoute(Name = ...)]`.

Exemplo:

```csharp
internal static class IdentityRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/identity";

    internal const string UserId = "userId";
    internal const string AccountancyIdParam = "accountancyId";

    internal const string DeleteUser =
        $"{BaseUri}/users/{{{UserId}:guid}}";

    internal const string PromoteAccountancyMember =
        $"{BaseUri}/accountancies/{{{AccountancyIdParam}:guid}}/members/{{{UserId}:guid}}/promote";
}
```

---

## Status de Resposta

Declare um `[ProducesResponseType]` para todo status que o endpoint pode retornar.

O projeto atual usa `200 OK` para creates bem-sucedidos. Use `201 Created` apenas
se a convenção da API for alterada intencionalmente.

Endpoints autorizados devem declarar:

- `401 Unauthorized`
- `403 Forbidden`

Adicione status específicos de negócio conforme necessário:

- `400 BadRequest` para validação e erros genéricos
- `404 NotFound` para recursos ausentes
- `409 Conflict` para conflitos de unicidade ou estado

Mantenha o texto visível no Swagger alinhado com `web-swagger.md`.

---

## HandleFailure

`HandleFailure` é definido em:

```text
src/Core/Core.Endpoints/Extensions/EndpointBaseExtensions.cs
```

Mapeamento atual:

- `IValidationResult` -> `400 Bad Request`
- `NotFoundError` -> `404 Not Found`
- `ConflictError` -> `409 Conflict`
- `ForbiddenError` -> `403 Forbidden`
- Any other `Error` -> `400 Bad Request`

Todas as respostas de erro usam `ProblemDetails`.

---

## Nunca Fazer

- Não coloque lógica de negócio em endpoints.
- Não passe DTOs de requisição diretamente para handlers.
- Não hardcode strings de rota.
- Não hardcode nomes de parâmetros de rota.
- Não omita possíveis códigos de status.
- Não coloque `[Authorize]` em `HandleAsync`.
- Não use string literals para tags Swagger.
- Não crie wording de Swagger aqui; use `web-swagger.md`.

---

## Verificação

Após mudanças em endpoints, rode:

```powershell
dotnet test tests/Web.Tests/
```

---