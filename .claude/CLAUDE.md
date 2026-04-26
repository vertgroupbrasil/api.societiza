# CLAUDE.md

Este arquivo fornece orientação ao Claude Code (claude.ai/code) ao trabalhar com código neste repositório.

## Skills

As skills do projeto ficam em `.claude/skills/`. Leia a skill relevante antes de iniciar qualquer tarefa:

- @.claude/skills/architecture/SKILL.md — obrigatória antes de tocar em qualquer `.cs` ou `.csproj`.
- @.claude/skills/infrastructure/SKILL.md — obrigatória antes de rodar migrations ou comandos Docker.

---

## Comandos de Build e Execução

### Desenvolvimento Local (Docker)

```bash
# Start infrastructure (PostgreSQL, MongoDB, RabbitMQ)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml up -d

# Build Docker image
docker build --no-cache -t societiza-api .

# Run API container
docker run --rm -p 5258:8080 --network societiza-dev --env-file .env.local societiza-api
```

- API: `http://localhost:5258/api`
- Swagger: `http://localhost:5258/api/swagger`

### .NET CLI

```bash
# Build solution
dotnet build Societiza.sln

# Run the web project
dotnet run --project src/Web/Web.csproj

# Run tests
dotnet test Societiza.sln

# Run a single test project
dotnet test tests/<TestProjectName>/

# Add EF migration
dotnet ef migrations add <MigrationName> \
  --project src/Modules/<Module>/<Module>.Persistence \
  --startup-project src/Web \
  --context <Module>DbContext

# Apply migrations to the database
dotnet ef database update \
  --project src/Modules/<Module>/<Module>.Persistence \
  --startup-project src/Web \
  --context <Module>DbContext

# Available contexts:
#   Accountancy      → AccountancyDbContext
#   Identity         → IdentityDbContext
#   WorkflowTemplate → WorkflowTemplateDbContext
```

### Configuração de Ambiente

Copie `.env.example` para `.env.local` e configure as strings de conexão antes de executar.

## Arquitetura

Este projeto é construído como um **Monólito Modular Orientado a Eventos**, usando os princípios de **DDD**, **CQRS** e **Clean Architecture**.

### Estrutura da Solução

```
src/
├── Core/          # Cross-cutting infrastructure (shared by all modules)
├── Modules/       # Feature modules (Accountancy, Identity, WorkflowTemplate)
└── Web/           # ASP.NET Core host (entry point)
tests/             # All test projects
```

Cada módulo e o Core têm 5 camadas: `Domain → Application → Infrastructure → Persistence → Shared`.

### Fluxo de Dados

```
HTTP Request
  → Web/Endpoints (Ardalis API Endpoints)
  → MediatR (CQRS handler)
  → Application layer (use cases, validation)
  → Domain logic
  → EF Core → PostgreSQL (writes)
  → Domain Events → MassTransit/RabbitMQ
  → Event Handlers → MongoDB projections (reads)
```

Para patterns detalhados de arquitetura, convenções de camada e a checklist de implementação de funcionalidade, consulte `.claude/skills/architecture/SKILL.md`.

### Bancos de Dados

- **PostgreSQL** — dados transacionais + jobs Hangfire (`societiza-postgres-dev:5432`)
- **MongoDB** — projections de leitura por módulo (`societiza-mongodb-dev:27017`)
- **RabbitMQ** — transporte do barramento de eventos (`societiza-rabbitmq-dev:5672`)

## Email

- **Provider**: Brevo via relay SMTP — sem SDK ou pacote externo. Escolhido pelo free tier generoso (300 emails/dia) e por não exigir domínio verificado inicialmente.
- **Integração**: apenas `System.Net.Mail.SmtpClient` nativo; não adicione pacotes externos de email.
- **Desenvolvimento**: use SMTP da Brevo com um email pessoal como remetente e destinatário para testes.
- **Produção**: relay SMTP da Brevo em `smtp-relay.brevo.com`, porta `587`, SSL habilitado.
- **Futuro**: quando `societiza.com.br` estiver disponível, atualize `FromEmail` para `noreply@societiza.com.br`.
- **Segurança**: a chave SMTP da Brevo **nunca** deve ser commitada. Armazene em `.env.local` (já está no `.gitignore`).

### Padrão de Uso

`IEmailService` é uma abstração genérica de entrega com um único método `SendAsync(toEmail, toName, subject, body, ct)`. Ele **não conhece conceitos de domínio** como "convite" ou "boas-vindas" — isso é responsabilidade de cada módulo.

**Nunca injete `IEmailService` diretamente em handlers de negócio.** O envio deve ser sempre assíncrono via Hangfire:

1. Cada módulo cria seus próprios comandos de email em `{Module}.Shared/Emails/` (record de comando) e `{Module}.Application/UseCases/Emails/` (handler)
2. Handlers de comando de email injetam `IEmailService`, montam o assunto e o corpo HTML, e chamam `SendAsync`
3. Handlers de negócio injetam `IJobSchedulerService` e enfileiram: `_jobScheduler.Enqueue(new SendXxxEmailCommand(...))`

```
// Per-module structure
{Module}.Shared/
└── Emails/
    └── Send{Action}EmailCommand.cs        // sealed record, primitives only (string, Guid)

{Module}.Application/
└── UseCases/Emails/
    └── Send{Action}EmailCommandHandler.cs // injects IEmailService, builds subject/body
```

O comando vive em `Shared` para que possa ser referenciado sem acoplamento à camada Application. Comandos de email devem conter **apenas tipos primitivos** em seus parâmetros para garantir a serialização do Hangfire.