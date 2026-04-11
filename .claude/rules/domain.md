# Rule — Domain

Escopo: backend

## Nunca fazer

- Criar `User` diretamente — apenas via `User.Create()` após `UserInvite` aceito
- Usar `WorkflowTemplate` com `Status = Draft` para instanciar processos
- Mutar `TemplateVersion` sem criar nova versão do template
- Usar conversão implícita de SmartEnum em domain events — sempre `.Name` explícito:
  `Role: role.Name` e não `Role: role`, mesmo que o compilador aceite
- Transportar tipo SmartEnum como campo de domain event record — campo é sempre `string`
- Chamar `When()` diretamente — só invocado por `ApplyEvent(IDomainEvent @event) => When(@event as dynamic)`
- Atribuir estado diretamente no aggregate fora de um `When()` handler
- Declarar constructor público em aggregate — usar constructor `private` implícito + `Create()`
- Espalhar domain events em múltiplos arquivos — todos em `{Module}.Domain/Events/DomainEvents.cs`
- Usar `Version` fixo em `RaiseEvent<T>()` — sempre usar o parâmetro do delegate

## Sempre fazer

- Usar `.Name` explícito ao passar SmartEnum para evento: `Status: InviteStatus.Pending.Name`
- Usar `FromName()` explícito ao reconstituir SmartEnum no `When()`: `Status = InviteStatus.FromName(@event.Status)`
- O par é sempre simétrico: `.Name` no `RaiseEvent` → `FromName()` no `When()`
- Transportar SmartEnum como `string` em projection models — nunca o tipo SmartEnum
- Incluir `Version` em todo domain event record
- Incluir `IsDeleted = false` como default em todo projection model record
- Validar `UserInvite.IsValid()` antes de aceitar um convite no handler
- Incluir `FieldIds`, `TaskIds` e `TaskFieldIds` no evento `WorkflowTemplateStepRemoved`
  para permitir deleção em cascata nas projections

## Razão

`.Name` explícito é preferível à conversão implícita porque torna a intenção
visível na leitura do código e cria simetria com `FromName()` na reconstituição.
A conversão implícita funciona mas cria dependência invisível — se removida do
SmartEnum, quebra silenciosamente em contextos de inferência de tipo.