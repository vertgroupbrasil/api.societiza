# Authentication — Societiza

> Documento técnico descrevendo a implementação completa de autenticação do módulo Identity.
> Gerado em 02/04/2026.

---

## 1. Visão Geral

O sistema de autenticação do Societiza é baseado em JWT (JSON Web Tokens) com rotação de refresh tokens. A implementação cobre cinco endpoints públicos/protegidos que permitem login, renovação de sessão, logout, solicitação de reset de senha e consumo do token de reset.

A autenticação se integra ao pipeline existente de Event Sourcing, projeções MongoDB e mensageria via RabbitMQ, mantendo consistência com a arquitetura Event-Driven Modular Monolith do projeto.

### Componentes implementados

- **TokenService** — geração e validação de JWTs (access + refresh)
- **CurrentUserService** — leitura de claims do HttpContext (zero I/O)
- **ExceptionHandlingMiddleware** — exceções de domínio semânticas (403, 401, 404)
- **HasRoleHandler + IdentityPolicies** — autorização por role via policies
- **RefreshToken / PasswordResetToken** — entidades de domínio com projeções MongoDB
- **5 endpoints de autenticação** — login, refresh, logout, forgot-password, reset-password

---

## 2. Endpoints

### POST /api/v1/identity/auth/login

**Autenticação:** Pública

**Fluxo:**
1. Recebe `email` e `password` no body
2. Busca o usuário na projeção MongoDB por email
3. Carrega o aggregate `User` do Event Store
4. Valida a senha com PBKDF2 (SHA-256, 100k iterações)
5. Gera access token + refresh token JWT
6. Faz SHA-256 do refresh token e persiste via `User.AddRefreshToken`
7. Retorna `{ accessToken }` no body
8. Seta cookie `refresh_token` (HttpOnly, Secure, SameSite=Strict, 7 dias)

**Segurança:** Erros de credenciais retornam sempre a mesma mensagem genérica (`"Invalid email or password"`) — nunca revela se o email existe.

---

### POST /api/v1/identity/auth/refresh

**Autenticação:** Pública (cookie `refresh_token`)

**Fluxo:**
1. Lê o refresh token do cookie HttpOnly
2. Valida o JWT do refresh token (assinatura, expiração, issuer, audience)
3. Extrai `sub` (UserId) das claims
4. Carrega o aggregate `User` do Event Store
5. Faz SHA-256 do token e busca no aggregate — valida `IsValid()`
6. **Rotação:** revoga o token atual, gera novo refresh token JWT
7. Gera novo access token
8. Retorna `{ accessToken }` no body + novo cookie `refresh_token`

**Segurança:** Rotação de refresh token — cada uso invalida o token anterior. Tokens já revogados ou expirados são rejeitados.

---

### POST /api/v1/identity/auth/logout

**Autenticação:** `[Authorize]` (Bearer token obrigatório)

**Fluxo:**
1. Lê o refresh token do cookie HttpOnly
2. Valida o JWT e extrai `sub`
3. Carrega o aggregate e revoga o refresh token correspondente
4. Deleta o cookie `refresh_token` da resposta

---

### POST /api/v1/identity/auth/forgot-password

**Autenticação:** Pública

**Fluxo:**
1. Recebe `email` no body
2. Busca o usuário na projeção MongoDB
3. Se não encontrado → **retorna 200 (silent success)**
4. Gera token aleatório (32 bytes, base64)
5. Faz SHA-256 e persiste via `User.AddPasswordResetToken` (expiração: 1 hora)
6. Enfileira email via Hangfire com o link de reset (`{frontendBaseUrl}/auth/reset-password?token={rawToken}`)
7. Retorna 200

**Segurança:** Sempre retorna 200 independente de o email existir — previne enumeração de contas. O token bruto viaja apenas no email; apenas o hash SHA-256 é persistido.

---

### POST /api/v1/identity/auth/reset-password

**Autenticação:** Pública (token no body)

**Fluxo:**
1. Recebe `token` e `newPassword` no body
2. Faz SHA-256 do token e busca na projeção `PasswordResetToken` por `TokenHash`
3. Carrega o aggregate `User` e valida o token (`IsValid()` — não usado e não expirado)
4. Marca o token como usado via `User.UsePasswordResetToken`
5. Hash da nova senha com PBKDF2 e atualiza via `User.UpdatePassword`
6. Retorna 200

**Segurança:** Token de uso único (marcado como `IsUsed` após consumo). Expiração de 1 hora. Senha mínima de 8 caracteres.

---

## 3. Modelo de Tokens

### Access Token (JWT)

| Propriedade | Valor |
|---|---|
| Expiração | 15 minutos |
| Claim `sub` | `User.Id` |
| Claim `email` | `User.Email` |
| Claim `role` | `User.Role.Name` |
| Claim `accountancy_id` | `User.AccountancyId` (omitido para SystemAdmin) |
| Algoritmo | HMAC-SHA256 |
| ClockSkew | Zero (sem tolerância) |

### Refresh Token (JWT)

| Propriedade | Valor |
|---|---|
| Expiração | 7 dias |
| Claim `sub` | `User.Id` |
| Claim `jti` | `Guid` único (permite busca direta) |
| Algoritmo | HMAC-SHA256 |
| Armazenamento | SHA-256 hash no Event Store + projeção MongoDB |
| Transporte | Cookie HttpOnly, Secure, SameSite=Strict |
| Rotação | Sim — cada uso revoga o anterior e emite novo |

### Pipeline JWT no ASP.NET

- `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()` — preserva nomes originais das claims
- Bearer scheme configurado no `Program.cs` com `TokenValidationParameters`
- `HasRoleHandler` valida a claim `role` contra as policies registradas em `IdentityPolicies`

---

## 4. Modelo de Reset de Senha

| Etapa | Detalhe |
|---|---|
| Geração | `RandomNumberGenerator.GetBytes(32)` → base64 |
| Hash | SHA-256 do token bruto → persistido como `TokenHash` |
| Expiração | 1 hora (`DateTimeOffset.UtcNow.AddHours(1)`) |
| Uso único | `IsUsed` flag — marcado após consumo |
| Envio | Via Hangfire (`SendPasswordResetEmailCommand`) — nunca síncrono |
| Link | `{FrontendOptions.BaseUrl}/auth/reset-password?token={rawToken}` |
| Silent success | Forgot-password retorna 200 mesmo para emails inexistentes |

---

## 5. Integração com a Arquitetura

### Event Sourcing

Todos os comportamentos de autenticação são eventos de domínio no aggregate `User`:

- `UserRefreshTokenAdded` — adição de refresh token
- `UserRefreshTokenRevoked` — revogação de refresh token
- `UserPasswordResetTokenAdded` — criação de token de reset
- `UserPasswordResetTokenUsed` — consumo do token de reset
- `UserPasswordUpdated` — atualização de senha

Os eventos são persistidos no Event Store (PostgreSQL) e reconstituídos via `User.LoadFromStream()`.

### Projeções MongoDB

Cada entidade tem sua projeção independente:

- `ProjectionModel.RefreshToken` — projetada por `ProjectionRefreshTokenEventHandler`
- `ProjectionModel.PasswordResetToken` — projetada por `ProjectionPasswordResetTokenEventHandler`
- `ProjectionModel.User` — atualizada com `UpdatedAt` em `UserPasswordUpdated`

### RabbitMQ (MassTransit)

Consumers registrados em `ConsumerConfiguration`:

- `ProjectionUserConsumer` — `UserRegistrationCompleted`, `UserPasswordUpdated`
- `ProjectionRefreshTokenConsumer` — `UserRefreshTokenAdded`, `UserRefreshTokenRevoked`
- `ProjectionPasswordResetTokenConsumer` — `UserPasswordResetTokenAdded`, `UserPasswordResetTokenUsed`

### Hangfire

O envio de email de reset de senha é enfileirado via `IJobSchedulerService.Enqueue(SendPasswordResetEmailCommand)` — nunca executado de forma síncrona no handler, conforme regra de negócio do Societiza.

---

## 6. Próximos Passos

Para considerar a autenticação 100% completa no contexto do roadmap:

1. **Handler de email de reset de senha** — implementar `SendPasswordResetEmailCommandHandler` em `Identity.Application/UseCases/Emails/` que injeta `IEmailService` e constrói o HTML do email
2. **Isolamento por tenant nos endpoints existentes** — aplicar `[Authorize]` e `ResolveAccountancyId` nos endpoints dos módulos Accountancy e WorkflowTemplate
3. **Proteção das rotas de convite** — aplicar as policies `CanInviteSystemAdmin`, `CanInviteAccountingAdmin`, etc. nos endpoints `InviteUser`, `ResendInvite`, `RevokeInvite`
4. **Aplicar a migration ao banco** — `dotnet ef database update` no `IdentityDbContext` (migration `AddAuthTokenEntities` já gerada, aguardando infraestrutura Docker)
5. **Seed do SystemAdmin** — criar o primeiro usuário SystemAdmin para bootstrap do sistema

---

*Gerado em 02/04/2026.*