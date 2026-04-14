---
name: api-design
description: >
  Required before creating or editing any endpoint class (files under
  src/Web/Endpoints/Modules/). Codifies the Societiza API style: Portuguese
  summaries and descriptions, hierarchical Tags, mandatory OperationId, required
  ProducesResponseType per operation type, and Description template (propósito,
  regras, efeitos, autorização). A convention test in tests/Web.Tests enforces
  these rules — following the skill is the path to passing the build.
---

# API Style Guide

Este guia padroniza **como** os endpoints são escritos para que o Swagger fique legível e o frontend gere clientes limpos. Regras estruturais (presença/formato) são validadas pelo teste `EndpointConventionsTests` em `tests/Web.Tests/`. Regras de conteúdo dependem de você seguir este documento ao escrever.

---

## 1. Idioma

**Português em tudo** — `Summary`, `Description`, e qualquer texto visível no Swagger. Nada em inglês.

---

## 2. `[SwaggerOperation]` — campos obrigatórios

Todo endpoint tem os 4 campos abaixo preenchidos:

```csharp
[SwaggerOperation(
    OperationId = "{NomeDaClasseSemSufixoEndpoint}",
    Summary     = "{Verbo no infinitivo + recurso, ≤ 60 chars}",
    Description = """
        {Propósito em 1 frase.}

        **Regras:** {unicidade, estados permitidos, validações específicas}
        **Efeitos:** {emails, eventos, soft-delete — ou "Nenhum"}
        **Autorização:** {role/policy exigida ou "Público"}
        """,
    Tags = [Tags.{Module}.{SubTag}])]
```

### 2.1. `OperationId`

**Regra determinística**: nome da classe sem o sufixo `Endpoint`.

| Classe | OperationId |
|---|---|
| `CreateAccountancyEndpoint` | `CreateAccountancy` |
| `GetAccountancyByIdEndpoint` | `GetAccountancyById` |
| `GetAllAccountanciesEndpoint` | `GetAllAccountancies` |
| `LoginEndpoint` | `Login` |

É daí que sai o nome da função no cliente TypeScript gerado. Tem que ser globalmente único.

### 2.2. `Summary`

- Verbo no infinitivo + recurso
- Máximo 60 caracteres
- Sem artigo desnecessário: "Criar contabilidade", não "Criar uma nova contabilidade"

Exemplos canônicos:

| Operação | Summary |
|---|---|
| Create | "Criar {recurso}" |
| Update | "Atualizar {recurso}" |
| Delete | "Excluir {recurso}" |
| Get by Id | "Obter {recurso} por ID" |
| List | "Listar {recursos}" |
| Ação de domínio | "Ativar {recurso}", "Arquivar {recurso}", "Aceitar convite" |
| Auth | "Fazer login", "Efetuar logout", "Renovar access token" |

### 2.3. `Description`

Sempre preenchido. Prose em PT cobrindo as 4 seções (use `"""..."""` multi-linha quando precisar de formatação):

```
{Propósito — 1 frase de negócio.}

**Regras:** {o que causa 4xx: unicidade, estados, validações}
**Efeitos:** {emails, eventos, soft-delete, shift de ordem, etc. Use "Nenhum" se não há.}
**Autorização:** {Exemplos: "SystemAdmin", "AccountingAdmin ou SystemAdmin", "Autenticado (tenant próprio via claim `accountancy_id`)", "Público"}
```

Se houver valores enumerados (ex: `fieldType`, `taskType`), liste-os dentro da seção **Regras** usando bullet list Markdown.

### 2.4. `Tags`

Sempre via constante de `src/Web/Endpoints/Tags.cs` — **nunca** string literal. Estrutura:

```csharp
Tags.Accountancy                       // "Accountancy"
Tags.Identity.Auth                     // "Identity / Auth"
Tags.Identity.Invites                  // "Identity / Invites"
Tags.WorkflowTemplate.Templates        // "Workflow Templates / Templates"
Tags.WorkflowTemplate.Steps            // "Workflow Templates / Steps"
Tags.WorkflowTemplate.StepTasks        // "Workflow Templates / Step Tasks"
Tags.WorkflowTemplate.StepFields       // "Workflow Templates / Step Fields"
Tags.WorkflowTemplate.StepTaskFields   // "Workflow Templates / Step Task Fields"
```

Swagger UI agrupa por `/`. Ao criar um novo sub-recurso, adicione uma nova constante em `Tags.cs`.

---

## 3. `[ProducesResponseType]` — obrigatório por tipo de operação

O teste valida que a lista abaixo está presente. Adicione outros status codes se a regra de negócio produzir (ex: `409 Conflict` em unicidade).

| Tipo | Obrigatórios | + se `[Authorize]` |
|---|---|---|
| **Create** | `200 (tipo)`, `400` | `401`, `403` |
| **Update** | `200`, `400`, `404` | `401`, `403` |
| **Delete** | `200`, `404` | `401`, `403` |
| **Get by Id** | `200 (tipo)`, `400`, `404` | `401`, `403` |
| **List** | `200 (tipo)`, `400` | `401`, `403` |
| **Ação de domínio** (Activate, Archive, Accept, Revoke, ...) | `200`, `400` + o que a regra exigir | `401`, `403` |

**Regra rígida**: se a classe tem `[Authorize(...)]`, declara **obrigatoriamente** `401` e `403`.

O endpoint de sucesso com corpo usa `ProducesResponseType(typeof(T), StatusCodes.Status200OK)`. Sem corpo, só `ProducesResponseType(StatusCodes.Status200OK)`.

---

## 4. `[Authorize]` vs `[AllowAnonymous]`

- Endpoints que exigem role específica: `[Authorize(Policy = IdentityPolicies.{Policy})]`
- Endpoints autenticados sem policy específica: `[Authorize]`
- Endpoints públicos intencionais (login, refresh, forgot/reset password, accept invite): **sem atributo** por padrão — o teste os cobre via allowlist explícita

Ao promover um endpoint público para autenticado, adicione `[Authorize(...)]` **e** remova-o da allowlist no teste.

---

## 5. Estrutura obrigatória da classe

```csharp
[Authorize(Policy = IdentityPolicies.CanManageX)]  // opcional
public sealed class {Verb}{Resource}Endpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<{Verb}{Resource}Request>
    .WithActionResult<{ResponseType}>    // ou .WithActionResult (sem corpo)
{
    [ApiVersion("1.0")]
    [HttpPost({Routes}.{RouteConstant})]  // Http{Get,Put,Patch,Delete}
    [ProducesResponseType(typeof({ResponseType}), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    // ... demais conforme tabela
    [SwaggerOperation(
        OperationId = "{Verb}{Resource}",
        Summary     = "{...}",
        Description = """...""",
        Tags = [Tags.{Module}.{SubTag}])]
    public override async Task<ActionResult<{ResponseType}>> HandleAsync(
        {Verb}{Resource}Request request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new {Verb}{Resource}Command(...))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
```

- Classe sempre `sealed` (Ardalis recomenda; consistente no projeto)
- Nome: `{Verb}{Resource}Endpoint` (o `Endpoint` é consumido pelo `OperationId`)
- Rota sempre vem de `Routes/{Module}Routes.cs` — nunca literal
- Atributo `[ApiVersion("1.0")]` obrigatório

---

## 6. Exemplo canônico

Veja `references/endpoint-template.cs` para um exemplo completo pronto-pra-copiar (Create com `[Authorize(Policy=...)]`).

---

## 7. Checklist mental ao criar/editar endpoint

- [ ] Classe `sealed`, nomeada `{Verb}{Resource}Endpoint`
- [ ] `[ApiVersion("1.0")]`
- [ ] `[Http*]` usando constante de `Routes/`
- [ ] Lista completa de `[ProducesResponseType]` conforme tabela do §3
- [ ] Se `[Authorize]`: 401 e 403 declarados
- [ ] `[SwaggerOperation]` com `OperationId`, `Summary` (≤60), `Description` (template §2.3), `Tags`
- [ ] `Tags` via constante de `src/Web/Endpoints/Tags.cs`
- [ ] Tudo em PT