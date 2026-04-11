# Command — add-migration

Gerar e aplicar migration EF Core por módulo no Societiza.

## Pré-condições

- [ ] Infraestrutura local rodando: `docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml up -d`
- [ ] `.env.local` configurado com connection strings
- [ ] Mudança de schema identificada — qual aggregate ou entidade foi alterada

## Inputs obrigatórios

- `{Nome}`: nome descritivo em PascalCase (ex: `AddUserInvitePasswordResetToken`)
- `{Modulo}`: Accountancy | Identity | WorkflowTemplate
- `{Contexto}`: AccountancyDbContext | IdentityDbContext | WorkflowTemplateDbContext

## Passos
```bash
# 1. Verificar containers
docker ps

# 2. Build — garantir que o código compila antes de gerar a migration
dotnet build Societiza.sln
dotnet test Societiza.sln

# 3. Gerar migration
dotnet ef migrations add {Nome} \
  --project src/Modules/{Modulo}/{Modulo}.Persistence \
  --startup-project src/Web \
  --context {Contexto}

# 4. Revisar o arquivo gerado em:
#    src/Modules/{Modulo}/{Modulo}.Persistence/Migrations/
#    Verificar Up() e Down() antes de aplicar

# 5. Aplicar
dotnet ef database update \
  --project src/Modules/{Modulo}/{Modulo}.Persistence \
  --startup-project src/Web \
  --context {Contexto}
```

## Referência rápida

| Módulo | Contexto |
|--------|---------|
| Accountancy | AccountancyDbContext |
| Identity | IdentityDbContext |
| WorkflowTemplate | WorkflowTemplateDbContext |

## Após aplicar

- [ ] Confirmar que `MigrateDatabaseStartupTask` já inclui o contexto do módulo
- [ ] Se novo aggregate: confirmar que existem `StoreEventConfiguration` e `SnapshotConfiguration`

## Skills requeridas

- `.claude/skills/infra/SKILL.md`