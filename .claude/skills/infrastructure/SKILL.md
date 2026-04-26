---
name: infrastructure
description: >
  Use esta skill ao rodar migrations EF Core (dotnet ef migrations add, dotnet ef database
  update), gerenciar o ambiente local Docker Compose (PostgreSQL, MongoDB, RabbitMQ),
  buildar ou executar a imagem Docker de produção, raciocinar sobre variáveis de ambiente,
  connection strings ou pipeline de CI/CD. Contém comandos exatos para todo DbContext
  (IdentityDbContext, AccountancyDbContext, WorkflowTemplateDbContext), a armadilha de
  carregamento de variável de ambiente que quebra dotnet ef, nomes e portas dos serviços
  Docker Compose, decisões do build em múltiplos estágios do Dockerfile e a estrutura da pipeline
  GitHub Actions. Leia esta skill antes de tocar em qualquer migration, arquivo .env,
  docker-compose ou Dockerfile.
---

# Infraestrutura

## Como Usar Esta Skill

Leia a seção que corresponde à sua tarefa. Nunca adivinhe comandos ou connection strings — este
arquivo tem os valores exatos.

- **EF Core / Migrations** — adicionar ou aplicar migrations para qualquer módulo
- **Docker Compose** — gerenciar a infraestrutura local (PostgreSQL, MongoDB, RabbitMQ)
- **Dockerfile** — entender a imagem de produção ou preparar deploy
- **CI/CD** — entender a pipeline GitHub Actions

---

## EF Core / Migrations

### DbContext por Módulo

Cada módulo tem seu próprio `DbContext` EF Core isolado em `{Module}.Persistence`. Não há
context compartilhado.

```
Identity         → IdentityDbContext         → src/Modules/Identity/Identity.Persistence
Accountancy      → AccountancyDbContext      → src/Modules/Accountancy/Accountancy.Persistence
WorkflowTemplate → WorkflowTemplateDbContext → src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence
```

Cada módulo também tem um `{Module}ProjectionDbContext` herdando de `MongoDbContext` — este
**não usa EF Migrations** e é gerenciado diretamente pelo MongoDB.

### Pattern de Comando

Todo comando `dotnet ef` exige três flags:

- `--project` → projeto `.Persistence` do módulo (contém o DbContext e as Migrations)
- `--startup-project` → sempre `src/Web` (onde as opções de conexão são configuradas)
- `--context` → nome explícito do DbContext (obrigatório porque `Web` referencia múltiplos contexts)

### Armadilha de Variável de Ambiente

`Program.cs` carrega `.env` (não `.env.local`) via `DotNetEnv`:

```csharp
if (File.Exists(".env"))
{
    Env.Load();
}
```

`dotnet ef` invoca o startup project pelo design-time host, então ele executa esse código.
Porém:

- `.env.local` **não é carregado automaticamente** — apenas `.env`
- Se você só tiver `.env.local`, o EF não encontrará as connection strings e falhará

**Solução** — exporte a variável obrigatória antes de rodar `dotnet ef`:

```bash
# PowerShell
$env:ConnectionStrings__Default = "Server=localhost;Port=5432;Username=societiza;Password=societiza;Database=societiza;"

# Alternatively: copy .env.local to .env at the repo root before running
# Do not commit .env — it is already in .gitignore
```

### Comandos de Migration

#### Identity

```bash
# Add migration
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Identity/Identity.Persistence \
  --startup-project src/Web \
  --context IdentityDbContext

# Apply to database
dotnet ef database update \
  --project src/Modules/Identity/Identity.Persistence \
  --startup-project src/Web \
  --context IdentityDbContext
```

#### Accountancy

```bash
# Add migration
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Accountancy/Accountancy.Persistence \
  --startup-project src/Web \
  --context AccountancyDbContext

# Apply to database
dotnet ef database update \
  --project src/Modules/Accountancy/Accountancy.Persistence \
  --startup-project src/Web \
  --context AccountancyDbContext
```

#### WorkflowTemplate

```bash
# Add migration
dotnet ef migrations add <MigrationName> \
  --project src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence \
  --startup-project src/Web \
  --context WorkflowTemplateDbContext

# Apply to database
dotnet ef database update \
  --project src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence \
  --startup-project src/Web \
  --context WorkflowTemplateDbContext
```

### Localização das Migrations

```
src/Modules/Identity/Identity.Persistence/Migrations/
src/Modules/Accountancy/Accountancy.Persistence/Migrations/
src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence/Migrations/
```

### O que NUNCA fazer — EF Core

- Nunca omita `--context` — `Web` referencia múltiplos DbContexts e o EF não conseguirá resolver o correto.
- Nunca rode `dotnet ef` sem antes garantir que a connection string está disponível no ambiente — ele falhará silenciosamente ou com erro enganoso.
- Nunca rode `dotnet ef` usando `{Module}.Persistence` como startup project — configuração e DI são conectados apenas em `src/Web`.
- Nunca crie ou aplique migrations para `{Module}ProjectionDbContext` — contexts MongoDB são schema-free e não usam EF Migrations.
- Nunca adicione um novo DbContext de módulo sem registrá-lo em `MigrateDatabaseStartupTask` — migrations não rodarão no startup.

---

## Docker Compose

### Arquivo

```
Docker/docker-compose.Infrastructure.Development.yaml
```

Inicia apenas dependências de infraestrutura — sem API. A API roda localmente via `dotnet run` ou como
container separado.

### Serviços

```
PostgreSQL → societiza-postgres-dev → localhost:5432
           → user: societiza / pass: societiza / db: societiza

MongoDB    → societiza-mongodb-dev  → localhost:27017
           → user: societiza / pass: societiza

RabbitMQ   → societiza-rabbitmq-dev → localhost:5672 (AMQP) + localhost:15672 (Management UI)
           → user: guest / pass: guest
```

- **Docker network**: `societiza-dev` — todos os containers compartilham essa network
- **Volumes**: `pg_data`, `mongodb_data`, `rabbitmq_data` (persistentes localmente)
- **Memory limit**: 512MB por serviço
- **RabbitMQ hostname**: `societiza-rmq` (usado internamente pelo broker)

### Comandos

```bash
# Start all infrastructure (detached)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml up -d

# Stop without removing volumes
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml down

# Stop and wipe volumes (full local state reset)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml down -v

# Check service status
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml ps

# Stream logs (all services)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml logs -f

# Stream logs (single service)
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml logs -f postgres

# Restart a single service
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml restart rabbitmq
```

### Strings de Conexão ao Rodar a API via `dotnet run`

`.env.local` usa nomes de container como hosts — eles resolvem apenas dentro da Docker network
`societiza-dev`. Ao rodar a API com `dotnet run` fora do Docker, use `localhost`:

```
ConnectionStrings__Default=Server=localhost;Port=5432;Username=societiza;Password=societiza;Database=societiza;
Projections__Identity=mongodb://societiza:societiza@localhost:27017/societiza?authSource=admin
EventBusOptions__ConnectionString=amqp://guest:guest@localhost:5672/
```

### O que NUNCA fazer — Docker Compose

- Nunca use nomes de host de container (`societiza-postgres-dev`, `societiza-mongodb-dev`, `societiza-rabbitmq-dev`) em strings de conexão ao rodar a API com `dotnet run` — eles resolvem apenas dentro da rede Docker.
- Nunca rode `down -v` a menos que pretenda apagar todos os dados locais — volumes são persistentes por design.

---

## Dockerfile

**Arquivo**: `Dockerfile` na raiz do repositório.

Usa um **build em múltiplos estágios** com dois estágios. As imagens base são `mcr.microsoft.com/dotnet/sdk:8.0-alpine`
para o estágio de build e `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` para o estágio de runtime.
Alpine foi escolhido para minimizar o tamanho da imagem.

### Estágio 1: `build`

**Imagem**: `mcr.microsoft.com/dotnet/sdk:8.0-alpine`

**`.csproj` e `.sln` são copiados primeiro** — decisão deliberada de cache de camadas Docker. Enquanto
os arquivos de projeto não mudarem, `dotnet restore` é pulado no rebuild.

**A cópia de código-fonte inclui** Core + todos os módulos (Accountancy, Identity, WorkflowTemplate) + Web.
O diretório `tests/` é excluído intencionalmente — projetos de teste não devem estar presentes na
imagem de produção.

**`dotnet restore src/Web/Web.csproj`** faz restore apenas a partir do ponto de entrada. EF Core Design é
declarado em `Web.csproj` com `PrivateAssets=all` para não vazar para a imagem de runtime.

**`dotnet publish src/Web/Web.csproj -c Release -o /app`** compila em modo Release e publica
todos os artefatos, incluindo dependências transitivas dos módulos, em `/app`.

Saída: `/app` com todos os binários publicados.

### Estágio 2: `runtime`

**Imagem**: `mcr.microsoft.com/dotnet/aspnet:8.0-alpine` — apenas runtime ASP.NET Core, sem SDK.

**Um usuário non-root é criado** (`appuser` no grupo `appgroup`) — o processo não roda como root
dentro do container.

**`ASPNETCORE_URLS=http://+:8080`** — a aplicação escuta na porta 8080. As portas 80 e 443 exigem root
e não podem ser usadas com usuário non-root. O mapeamento de porta externa (`-p 5258:8080`) é feito no
`docker run`.

**`EXPOSE 8080`** documenta a porta exposta — apenas metadado, não faz bind.

**Apenas `/app` é copiado do estágio `build`** — nenhum SDK, nenhum código-fonte, nenhum arquivo `.csproj`
chega à imagem final.

**`USER appuser`** troca para o usuário non-root antes do entrypoint.

**`ENTRYPOINT ["dotnet", "Web.dll"]`** invoca o assembly publicado diretamente.

Saída: imagem de produção enxuta, sem SDK e sem código-fonte.

### Variáveis de Ambiente de Runtime

A imagem não embute segredos. Toda configuração sensível deve ser injetada via `--env-file` ou pelo
gerenciamento de segredos do orquestrador:

```
ASPNETCORE_ENVIRONMENT            → Development / Production
ConnectionStrings__Default        → PostgreSQL — transactional + Hangfire
ConnectionStrings__Hangfire       → PostgreSQL — Hangfire jobs
Projections__Identity             → MongoDB connection string
Projections__Accountancy          → MongoDB connection string
Projections__WorkflowTemplate     → MongoDB connection string
EventBusOptions__ConnectionString → RabbitMQ AMQP URI
EmailOptions__Username            → Brevo SMTP username
EmailOptions__Password            → Brevo SMTP key
EmailOptions__FromEmail           → Sender address
JwtOptions__*                     → JWT secret, issuer, audience
Cors__AllowedOrigins__0           → Allowed CORS origins
```

### Comandos Docker

```bash
# Build image
docker build --no-cache -t societiza-api .

# Build with version tag
docker build --no-cache -t societiza-api:1.0.0 .

# Run container connected to dev network (infra must be up)
docker run --rm \
  -p 5258:8080 \
  --network societiza-dev \
  --env-file .env.local \
  societiza-api

# Run in detached mode
docker run -d \
  --name societiza-api \
  -p 5258:8080 \
  --network societiza-dev \
  --env-file .env.local \
  societiza-api

# Stream container logs
docker logs -f societiza-api

# Stop and remove
docker stop societiza-api && docker rm societiza-api
```

Após `docker run`:
- API: `http://localhost:5258/api`
- Swagger: `http://localhost:5258/api/swagger` (somente em `Development`)

### O que NUNCA fazer — Dockerfile

- Nunca fixe segredos ou strings de conexão no Dockerfile — injete via `--env-file` ou orquestrador.
- Nunca copie `tests/` para o estágio de build — projetos de teste não devem estar presentes na imagem de produção.
- Nunca altere `ASPNETCORE_URLS` para porta 80 ou 443 — elas exigem root e quebrariam a configuração de usuário non-root.
- Nunca promova o estágio de runtime de `aspnet` para `sdk` — o SDK não deve estar presente na imagem de produção.
- Nunca remova a instrução `USER appuser` — rodar como root é regressão de segurança.
- Nunca remova `PrivateAssets=all` do EF Core Design em `Web.csproj` — sem isso, ferramentas EF Design vazam para a imagem de runtime.

---

## CI/CD — GitHub Actions

**Pipeline**: `.github/workflows/societiza_bff_web.yml`

```
push on any branch
  └── build   (dotnet restore + dotnet build --configuration Release)
       └── test    (dotnet test --configuration Release)
            └── docker  (docker build --no-cache -t societiza-api .)
```

- **Build e test** rodam em `ubuntu-latest` com .NET 8.x
- **Step Docker** valida que a imagem builda com sucesso — não faz push para nenhum registry
- Push para registry de produção ainda não está implementado no CI
