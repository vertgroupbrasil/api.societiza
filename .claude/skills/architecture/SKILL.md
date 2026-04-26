---
name: architecture
description: >
  Use esta skill para alterações de código no repositório Societiza: funcionalidades,
  correções de bugs, refactors, migrations e limpezas. Este é o roteador de
  arquitetura para Domain, Shared, Application, Persistence, Infrastructure e Web.
  Ele aponta para os arquivos de referência específicos que contêm os patterns
  obrigatórios de cada camada.
---

# Base de Arquitetura

Esta skill é o ponto de entrada para as regras de arquitetura do Societiza.

Ela deve permanecer compacta, mas não vazia. Sua função é explicar o formato do
repositório, direcionar o agente para as referências corretas e evitar orientação
duplicada entre documentos.

Antes de implementar qualquer coisa, leia o arquivo de referência da camada que
será alterada. Não confie em convenções genéricas de .NET quando existir uma
referência do projeto.

## Estrutura da Solução

```text
src/
+-- Core/                         # Cross-cutting infrastructure shared by modules
+-- Modules/
|   +-- {Module}.Domain/          # Aggregates, SmartEnums, Domain Events, Projection Models
|   +-- {Module}.Application/     # Use cases, handlers, validators, projection handlers
|   +-- {Module}.Infrastructure/  # Module installers and external service wiring
|   +-- {Module}.Persistence/     # EF Core, Event Store, MongoDB projections
|   +-- {Module}.Shared/          # Commands, queries, response DTOs, email/job contracts
+-- Web/                          # ASP.NET Core host: endpoints, routes, consumers, startup tasks
```

Não liste nomes concretos de módulos aqui. Módulos mudam ao longo do tempo; esta
skill documenta o formato arquitetural, não o inventário atual.

## Referências de Camada

- `references/domain-architecture.md` - Aggregates, Domain Events, SmartEnum, Projection Models.
- `references/shared-architecture.md` - Comandos, comandos de email, comandos de job, queries, DTOs de resposta.
- `references/application-architecture.md` - Handlers, validators, projection handlers, email handlers, job handlers.
- `references/persistence-architecture.md` - Event Store, configurações EF Core, projections MongoDB.
- `references/infrastructure-architecture.md` - ModuleInstaller, ServiceInstallers, scan do Scrutor, ApplicationService.
- `references/checklist.md` - Checklist de implementação end-to-end de funcionalidade.

Comece por `references/domain-architecture.md` ao modelar uma nova funcionalidade.

Comece por `references/checklist.md` ao implementar um modelo já definido de
ponta a ponta.

## Referências de Web

A referência antiga de Web foi dividida intencionalmente por responsabilidade.

Se criar ou editar um endpoint, leia ambos:

- `references/web-endpoints.md` - Estrutura da classe de endpoint, DTOs de requisição, rotas, autorização, declarações de status, pipeline railway.
- `references/web-swagger.md` - Texto do Swagger, OperationId, Summary, Description, Tags, wording de resposta.

Se criar ou editar consumers MassTransit, leia:

- `references/web-consumers.md` - Classes consumer, ConsumerConfiguration, nomenclatura de filas, registro de endpoint de recebimento de evento.

Se criar ou editar startup tasks, leia:

- `references/web-startup.md` - Startup tasks hospedadas, sinal de migration, jobs recorrentes, índices de projection, seed de desenvolvimento.

## Regras de Responsabilidade

- Domain é dona das invariantes de negócio e dos domain events.
- Shared é dona dos contratos que podem cruzar fronteiras de camada.
- Application é dona dos casos de uso e da orquestração.
- Persistence é dona dos mapeamentos de banco, da persistência de eventos e das projections.
- Infrastructure é dona dos serviços externos e do registro de módulos/serviços.
- Web é dona apenas dos adapters HTTP/barramento de eventos.

## Regras de Documentação

- Mantenha este arquivo como roteador.
- Mantenha regras detalhadas de camada nos arquivos de referência.
- Mantenha a estrutura de endpoints em `web-endpoints.md`.
- Mantenha o wording de Swagger em `web-swagger.md`.
- Não duplique exemplos grandes de endpoint em múltiplas referências.
- Não liste módulos concretos atuais nesta skill.
- Atualize as referências quando as convenções do projeto mudarem.
