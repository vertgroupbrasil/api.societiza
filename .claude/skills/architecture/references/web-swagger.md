# Web Swagger

> Swagger é o contrato HTTP público da API e a fonte para clients gerados. Mantenha-o estável, legível e determinístico.
> Este arquivo é dono do wording e dos metadados do Swagger. A estrutura das classes de endpoint pertence a `web-endpoints.md`.

---

## Validação

As convenções são validadas por:

```text
tests/Web.Tests/Endpoints/EndpointConventionsTests.cs
```

O teste atualmente verifica:

- `[ApiVersion]` on `HandleAsync`
- `[SwaggerOperation]` com `OperationId`, `Summary`, `Description` e exatamente uma tag
- comprimento de `Summary` até 60 caracteres
- prefixes de tag conhecidos
- pelo menos um `[ProducesResponseType]` de sucesso
- `401` e `403` quando `[Authorize]` está na classe
- `OperationId` globalmente único

---

## Idioma

Use português em todo texto visível do Swagger:

- `Summary`
- `Description`
- explicações de resposta
- regras de negócio
- notas de autorização

Não misture inglês em texto de Swagger, exceto ao nomear um token técnico, claim,
enum, header, cookie, rota ou valor de código.

---

## OperationId

Use o nome da classe de endpoint sem o sufixo `Endpoint`.

Exemplos:

| Class | OperationId |
|---|---|
| `CreateAccountancyEndpoint` | `CreateAccountancy` |
| `GetAccountancyByIdEndpoint` | `GetAccountancyById` |
| `RegisterUserEndpoint` | `RegisterUser` |
| `DeleteUserEndpoint` | `DeleteUser` |

`OperationId` deve ser globalmente único.

---

## Summary

Regras:

- Português.
- Máximo de 60 caracteres.
- Verbo no infinitivo mais recurso.
- Sem artigos de enchimento.
- Nome de ação claro para operações de domínio.

Exemplos:

| Operation | Summary |
|---|---|
| Create | `Criar contabilidade` |
| Update | `Atualizar contabilidade` |
| Delete | `Excluir usuário` |
| Get by id | `Obter contabilidade por ID` |
| List | `Listar links de convite` |
| Auth | `Fazer login` |
| Domain action | `Promover membro a administrador da contabilidade` |

---

## Description

Use uma raw string multi-linha.

Estrutura canônica:

```csharp
Description = """
    {Propósito em uma frase.}

    **Regras:**
    - {regra 1}
    - {regra 2}

    **Respostas:**
    - `200` - {resultado de sucesso}.
    - `400` - {erro de validação ou regra}.
    - `401` - access token ausente ou inválido.
    - `403` - usuário sem permissão.
    - `404` - recurso não encontrado.
    - `409` - conflito de estado ou unicidade.

    **Efeitos:** {efeitos colaterais ou "Nenhum."}

    **Autorização:** {Público. | Qualquer usuário autenticado. | papel/policy exigido.}

    **Observações:** {opcional. Use para cookies, fluxos e recomendações de uso.}
    """,
```

Regras:

- Mantenha uma linha em branco entre seções.
- Comece com uma frase de propósito de negócio.
- Use `**Regras:**` para regras de negócio e regras de validação.
- Use `**Respostas:**` para todo status declarado por `[ProducesResponseType]`.
- Use `**Efeitos:**` para efeitos colaterais como eventos, emails, cookies, soft-delete, revogação de token ou reordenação.
- Use `**Autorização:**` para requisitos públicos/autenticados/policy.
- Use `**Observações:**` apenas quando agregar valor operacional.
- Se não houver regras, escreva `**Regras:** Nenhuma.`
- Não crie títulos de seção livres quando uma seção existente se encaixa.

---

## Texto de Resposta

A seção `Respostas` deve corresponder aos status declarados.

Texto comum:

- `200` - sucesso; mencione o corpo retornado quando houver.
- `400` - dados inválidos, dados ausentes, query inválida ou regra violada mapeada para Bad Request.
- `401` - token de acesso ausente, inválido ou expirado.
- `403` - token válido sem permissão, claim de tenant ausente ou estado de domínio proibido.
- `404` - recurso solicitado não encontrado.
- `409` - recurso duplicado, conflito de unicidade ou transição de estado inválida.

Não documente um status que não está declarado.

Não declare um status sem documentá-lo.

---

## Tags

Sempre use constantes de:

```text
src/Web/Endpoints/Tags.cs
```

Nunca use strings literais em endpoints.

Tags atuais:

```csharp
Tags.Accountancy                       // "Accountancy"
Tags.Identity.Auth                     // "Identity / 01. Auth"
Tags.Identity.Invites                  // "Identity / 02. Invites"
Tags.Identity.Users                    // "Identity / 03. Users"
Tags.WorkflowTemplate.Templates        // "Workflow Templates / 01. Templates"
Tags.WorkflowTemplate.Steps            // "Workflow Templates / 02. Steps"
Tags.WorkflowTemplate.StepTasks        // "Workflow Templates / 03. Step Tasks"
Tags.WorkflowTemplate.StepFields       // "Workflow Templates / 04. Step Fields"
Tags.WorkflowTemplate.StepTaskFields   // "Workflow Templates / 05. Step Task Fields"
```

O prefixo numérico controla a ordenação do Swagger.

Adicione uma nova tag apenas quando houver um novo grupo visível no Swagger.

---

## Matriz de Status

Use isto como ponto de partida. Adicione qualquer status que o endpoint realmente possa retornar.

| Tipo de endpoint | Status obrigatórios | Adicionar quando autorizado |
|---|---|---|
| Criar | `200`, `400` | `401`, `403` |
| Atualizar | `200`, `400`, `404` | `401`, `403` |
| Excluir | `200`, `400`, `404` | `401`, `403` |
| Obter por id | `200`, `400`, `404` | `401`, `403` |
| Listar | `200`, `400` | `401`, `403` |
| Ação de domínio | `200`, `400` mais status específicos da regra | `401`, `403` |

Use `409` para conflitos como CNPJ duplicado, email duplicado, link de convite
esgotado, estado de promoção inválido ou unicidade de link ativo.

---

## Exemplos Canônicos

Create público:

```csharp
[SwaggerOperation(
    OperationId = "RegisterUser",
    Summary     = "Cadastrar usuário via link de convite",
    Description = """
        Cria uma nova conta a partir de um link de convite válido e retorna o identificador do usuário criado.

        **Regras:**
        - O token deve corresponder a um link ativo, não expirado e não esgotado.
        - Retorna 409 se já existir uma conta com o mesmo e-mail.
        - Senha deve ter no mínimo 8 caracteres.
        - ConfirmPassword deve ser igual a Password.

        **Respostas:**
        - `200` - usuário criado; retorna o identificador gerado.
        - `400` - dados inválidos, senha fora dos requisitos ou link expirado, esgotado ou revogado.
        - `404` - link de convite não encontrado.
        - `409` - já existe uma conta com o mesmo e-mail.

        **Efeitos:** Nenhum.

        **Autorização:** Público.
        """,
    Tags = [Tags.Identity.Users])]
```

Comando autorizado:

```csharp
[SwaggerOperation(
    OperationId = "DeleteUser",
    Summary     = "Excluir usuário",
    Description = """
        Realiza o soft-delete de um usuário e invalida imediatamente sua sessão.

        **Regras:**
        - SystemAdmin e AccountancyAdmin podem excluir usuários.
        - AccountancyAdmin só pode excluir membros da própria contabilidade.
        - Não é possível excluir a si mesmo.
        - Não é possível excluir o último AccountancyAdmin de uma contabilidade.
        - Não é possível excluir o último SystemAdmin do sistema.

        **Respostas:**
        - `200` - usuário excluído com sucesso.
        - `400` - dados inválidos ou ausentes.
        - `401` - access token ausente ou inválido.
        - `403` - usuário sem permissão para excluir o alvo informado.
        - `404` - usuário não encontrado.
        - `409` - exclusão violaria uma regra de último administrador.

        **Efeitos:** Tokens de refresh revogados e security stamp regenerado.

        **Autorização:** SystemAdmin ou AccountancyAdmin.
        """,
    Tags = [Tags.Identity.Users])]
```

---

## Checklist

- `OperationId` é igual ao nome da classe sem `Endpoint`.
- `Summary` está em português e tem até 60 caracteres.
- `Description` segue a estrutura canônica.
- `Respostas` corresponde a `[ProducesResponseType]`.
- `Tags` usa uma constante de `Tags.cs`.
- Endpoints autorizados documentam `401` e `403`.
- Endpoints públicos dizem `**Autorização:** Público.`
- O texto é útil para consumers frontend.

---