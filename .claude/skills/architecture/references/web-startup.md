# Startup Web

> Startup tasks são classes `BackgroundService` que rodam quando o host Web inicia.
> Use-as apenas para bootstrap em nível de host.

---

## Layout de Arquivos

```text
src/Web/
+-- StartupTasks/
|   +-- DatabaseMigrationSignal.cs
|   +-- MigrateDatabaseStartupTask.cs
|   +-- RegisterRecurringJobsStartupTask.cs
|   +-- EnsureProjectionIndexesStartupTask.cs
|   +-- SeedDevelopmentDataStartupTask.cs
+-- ServiceInstallers/StartupTasks/
    +-- StartupTasksServiceInstaller.cs
```

---

## Registro

Startup tasks são registradas em:

```text
src/Web/ServiceInstallers/StartupTasks/StartupTasksServiceInstaller.cs
```

Registros atuais:

```csharp
services.AddSingleton<DatabaseMigrationSignal>();
services.AddHostedService<MigrateDatabaseStartupTask>();
services.AddHostedService<RegisterRecurringJobsStartupTask>();
services.AddHostedService<EnsureProjectionIndexesStartupTask>();
```

`SeedDevelopmentDataStartupTask` existe, mas não está registrado atualmente.

---

## Migrations de Banco de Dados

`MigrateDatabaseStartupTask` executa migrations EF para todo DbContext de módulo:

- `AccountancyDbContext`
- `IdentityDbContext`
- `WorkflowTemplateDbContext`

Depois que as migrations terminam, ele sinaliza `DatabaseMigrationSignal`.

Regras:

- Adicione aqui todo novo DbContext de módulo.
- Use `MigrateAsync` quando existirem migrations.
- Use `EnsureCreatedAsync` apenas quando não existirem migrations.
- Não execute migrations a partir de handlers de requisição.

---

## Sinal de Migration

`DatabaseMigrationSignal` coordena startup tasks que precisam aguardar
migrations.

Use-o quando uma startup task depende de estado relacional migrado.

Exemplo:

```csharp
await migrationSignal.WaitAsync(stoppingToken);
```

---

## Jobs Recorrentes

`RegisterRecurringJobsStartupTask` registra jobs recorrentes do Hangfire.

Job atual:

```csharp
jobScheduler.ScheduleRecurring(
    "expire-pending-invitation-links",
    "0 * * * *",
    new ExpireInvitationLinksCommand());
```

Regras:

- Registre jobs recorrentes aqui.
- Use IDs de job estáveis.
- Use records de comando de `Shared`.
- Não agende jobs recorrentes a partir de handlers ou services.

---

## Índices de Projection

`EnsureProjectionIndexesStartupTask` cria índices de projection no MongoDB.

A implementação atual resolve:

```csharp
IIdentityProjectionDbContext
```

Regras:

- Adicione inicialização de índices aqui ou em uma startup task equivalente.
- Mantenha definições de índice em Persistence.
- Não crie índices a partir de código de endpoint.

---

## Seed de Desenvolvimento

`SeedDevelopmentDataStartupTask` cria um SystemAdmin de desenvolvimento se habilitado.

Atualmente ele:

- Aguarda `DatabaseMigrationSignal`.
- Verifica usuários ativos existentes.
- Cria `admin@societiza.dev`.
- Faz append de eventos por meio de `IIdentityApplicationService`.

Como ele não está registrado, não assuma que roda localmente a menos que o
registro seja adicionado intencionalmente.

---

## Nunca Fazer

- Não registre trabalho de startup ad hoc em `Program.cs`.
- Não rode workflows de negócio longos a partir de startup tasks.
- Não contorne `IJobSchedulerService` para jobs recorrentes.
- Não faça seed de dados de produção a partir de startup tasks Web.
- Não adicione um módulo DbContext sem atualizar o comportamento de startup de migration.

---