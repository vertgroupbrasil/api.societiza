---
name: api-design
description: >
  Obrigatória antes de criar ou editar qualquer endpoint em
  src/Web/Endpoints/Modules/. Codifica o estilo de API do Societiza:
  idioma PT, Summary/Description/OperationId/Tags obrigatórios, ProducesResponseType
  por tipo de operação, e estrutura canônica da classe. As regras estruturais são
  validadas automaticamente pelo teste EndpointConventionsTests na pipeline.
---

# api-design

## Propósito

Todo endpoint exposto pelo Societiza precisa ser legível no Swagger e gerar
clientes TypeScript limpos. Sem um padrão único, o Swagger vira uma mistura de
idiomas, IDs duplicados e descrições vazias — e o frontend não consegue gerar
código confiável a partir dele.

Esta skill define **como** cada endpoint deve ser escrito: idioma, campos
obrigatórios, convenções de nomeação e estrutura de classe. Ela não descreve
o que o endpoint faz — isso é tarefa da spec e do plano técnico.

Concretamente, a skill garante que:

- Toda documentação Swagger fique em português e siga um template consistente.
- O `OperationId` seja derivável de forma determinística do nome da classe.
- A lista de `ProducesResponseType` seja completa e correta para cada tipo de operação.
- Os erros de convenção sejam detectados automaticamente na pipeline, antes do PR.

---

## Dependência: teste de convenções

As regras estruturais desta skill são validadas por reflection no teste
`EndpointConventionsTests` em `tests/Web.Tests/Endpoints/EndpointConventionsTests.cs`.

O teste verifica, para cada endpoint em `Web.Endpoints.Modules.*`:

- `[ApiVersion]` presente no método `HandleAsync`
- `[SwaggerOperation]` com `OperationId`, `Summary` (≤ 60 chars), `Description` e exatamente uma `Tags`
- Tag com prefixo reconhecido (Accountancy, Identity / , Workflow Templates / )
- Pelo menos um `[ProducesResponseType]` de sucesso (200, 201 ou 204)
- Se `[Authorize]` na classe → 401 e 403 declarados no método
- `OperationId` globalmente único

**Seguir esta skill é o caminho para passar o build.** Qualquer endpoint que viole
as regras acima fará a pipeline falhar.

---

## Como usar esta skill

Leia esta skill **antes** de criar ou editar qualquer arquivo endpoint em
`src/Web/Endpoints/Modules/`. Não é necessário ler outras skills antes desta —
ela é autocontida para o propósito de escrever endpoints.

Se você é o agente `backend-engineer`, leia também `web-architecture.md` da
skill `/architecture` para o contexto de registro e roteamento.

---

## Convenções obrigatórias

### 1. Idioma

**Português em tudo** — `Summary`, `Description`, e qualquer texto visível no Swagger. Nada em inglês.

---

### 2. `[SwaggerOperation]` — campos obrigatórios

Todo endpoint tem os 4 campos abaixo preenchidos no método `HandleAsync`:

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

#### 2.1. `OperationId`

**Regra determinística**: nome da classe sem o sufixo `Endpoint`.

| Classe | OperationId |
|---|---|
| `CreateAccountancyEndpoint` | `CreateAccountancy` |
| `GetAccountancyByIdEndpoint` | `GetAccountancyById` |
| `GetAllAccountanciesEndpoint` | `GetAllAccountancies` |
| `LoginEndpoint` | `Login` |

É daí que sai o nome da função no cliente TypeScript gerado. Tem que ser globalmente único.

#### 2.2. `Summary`

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

#### 2.3. `Description`

Sempre preenchido. Prosa em PT cobrindo as 4 seções (use `"""..."""` multi-linha quando precisar de formatação):

```
{Propósito — 1 frase de negócio.}

**Regras:** {o que causa 4xx: unicidade, estados, validações}
**Efeitos:** {emails, eventos, soft-delete, shift de ordem, etc. Use "Nenhum" se não há.}
**Autorização:** {Exemplos: "SystemAdmin", "AccountingAdmin ou SystemAdmin", "Autenticado (tenant próprio via claim `accountancy_id`)", "Público"}
```

Se houver valores enumerados (ex: `fieldType`, `taskType`), liste-os dentro da seção **Regras** usando bullet list Markdown.

#### 2.4. `Tags`

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

### 3. `[ProducesResponseType]` — obrigatório por tipo de operação

O teste valida que a lista abaixo está presente no método `HandleAsync`. Adicione outros status codes se a regra de negócio produzir (ex: `409 Conflict` em unicidade).

| Tipo | Obrigatórios | + se `[Authorize]` na classe |
|---|---|---|
| **Create** | `200 (tipo)`, `400` | `401`, `403` |
| **Update** | `200`, `400`, `404` | `401`, `403` |
| **Delete** | `200`, `404` | `401`, `403` |
| **Get by Id** | `200 (tipo)`, `400`, `404` | `401`, `403` |
| **List** | `200 (tipo)`, `400` | `401`, `403` |
| **Ação de domínio** (Activate, Archive, Accept, Revoke, ...) | `200`, `400` + o que a regra exigir | `401`, `403` |

**Regra rígida**: se a classe tem `[Authorize(...)]`, declara **obrigatoriamente** `401` e `403` no método `HandleAsync`.

O endpoint de sucesso com corpo usa `ProducesResponseType(typeof(T), StatusCodes.Status200OK)`. Sem corpo, só `ProducesResponseType(StatusCodes.Status200OK)`.

---

### 4. `[Authorize]` vs público

- Endpoints que exigem role específica: `[Authorize(Policy = IdentityPolicies.{Policy})]` na **classe**
- Endpoints autenticados sem policy específica: `[Authorize]` na **classe**
- Endpoints públicos intencionais (login, refresh, forgot/reset password, accept invite): **sem atributo** — o teste os cobre via ausência de `[Authorize]`

Ao promover um endpoint público para autenticado, adicione `[Authorize(...)]` na **classe** e certifique-se de que 401 e 403 estão no método.

---

### 5. Estrutura obrigatória da classe

```csharp
[Authorize(Policy = IdentityPolicies.CanManageX)]  // opcional — nível de classe
public sealed class {Verb}{Resource}Endpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<{Verb}{Resource}Request>
    .WithActionResult<{ResponseType}>    // ou .WithActionResult (sem corpo)
{
    [ApiVersion("1.0")]
    [HttpPost({Routes}.{RouteConstant})]  // Http{Get,Put,Patch,Delete}
    [ProducesResponseType(typeof({ResponseType}), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    // ... demais conforme tabela do §3
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

- Classe sempre `sealed`
- Nome: `{Verb}{Resource}Endpoint` (o `Endpoint` é consumido pelo `OperationId`)
- Rota sempre vem de `Routes/{Module}Routes.cs` — nunca literal
- `[ApiVersion("1.0")]` obrigatório no método

---

### 6. Exemplo canônico

Veja `references/endpoint-template.cs` para um exemplo completo pronto-pra-copiar (Create com `[Authorize(Policy=...)]`).

---

## Checklist de criação/edição

- [ ] Classe `sealed`, nomeada `{Verb}{Resource}Endpoint`
- [ ] `[Authorize]` na classe (se necessário) — nunca no método
- [ ] `[ApiVersion("1.0")]` no método `HandleAsync`
- [ ] `[Http*]` usando constante de `Routes/`
- [ ] Lista completa de `[ProducesResponseType]` no método conforme tabela do §3
- [ ] Se `[Authorize]` na classe: 401 e 403 declarados no método
- [ ] `[SwaggerOperation]` com `OperationId`, `Summary` (≤ 60), `Description` (template §2.3), `Tags`
- [ ] `Tags` via constante de `src/Web/Endpoints/Tags.cs`
- [ ] Tudo em PT

---

## Nunca fazer

- Escrever `Summary`, `Description` ou qualquer texto Swagger em inglês
- Usar string literal em `Tags` — sempre via `Tags.{Module}.{SubTag}`
- Criar `OperationId` fora da regra `{NomeDaClasseSemSufixoEndpoint}`
- Omitir `[ApiVersion("1.0")]` ou colocá-lo na classe em vez do método
- Omitir `[ProducesResponseType(401)]` e `[ProducesResponseType(403)]` em endpoints com `[Authorize]`
- Colocar `[Authorize]` no método em vez da classe
- Usar rota literal no `[Http*]` — sempre via constante de `Routes/`
- Criar nova constante de tag sem adicioná-la em `Tags.cs`
- Deixar `Description` vazio ou com placeholder não preenchido

## Sempre fazer

- Ler esta skill antes de criar ou editar qualquer arquivo endpoint
- Seguir o template do §5 e o exemplo em `references/endpoint-template.cs`
- Colocar `[ApiVersion]`, `[Http*]`, `[ProducesResponseType]` e `[SwaggerOperation]` no método `HandleAsync`
- Preencher as 4 seções da `Description`: propósito, Regras, Efeitos, Autorização
- Verificar unicidade do `OperationId` contra os endpoints existentes antes de confirmar
- Rodar `dotnet test tests/Web.Tests/` localmente após criar ou editar endpoints
