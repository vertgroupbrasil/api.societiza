# Tarefa 6: Handler de Email de Reset de Senha

> Pendência identificada após a conclusão da Tarefa 5 (Endpoints de Autenticação).
> Cobre a implementação do `SendPasswordResetEmailCommandHandler`, única entrega necessária para fechar o fluxo de reset de senha end-to-end.
> Gerado em 02/04/2026.

---

## Visão Geral

O endpoint `POST /api/v1/identity/auth/forgot-password` já está implementado e enfileira o `SendPasswordResetEmailCommand` via Hangfire. O command e o command record existem em `Identity.Shared/Emails/`. O que falta é o handler que consome esse command, injeta `IEmailService` e envia o email.

---

## Dependências

Nenhuma alteração em código existente é necessária. O handler se conecta às peças já implementadas:

- `SendPasswordResetEmailCommand` — já existe em `Identity.Shared/Emails/`
- `IEmailService` — já registrado no `Core`
- `ForgotPasswordHandler` — já enfileira o command via `IJobSchedulerService`

---

## Estrutura de Arquivos

```
Identity.Application/
└── UseCases/
    └── Emails/
        ├── SendInviteEmailHandler.cs              ← já existe (referência de padrão)
        └── SendPasswordResetEmailCommandHandler.cs ← novo
```

---

## Implementação

**Localização:** `src/Modules/Identity/Identity.Application/UseCases/Emails/SendPasswordResetEmailCommandHandler.cs`

Segue o mesmo padrão do `SendInviteEmailHandler` — classe pública não-sealed, `BuildBody` como método estático privado, HTML minimalista sem estrutura de documento completa.

```csharp
using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Shared.Results;
using Identity.Shared.Emails;

namespace Identity.Application.UseCases.Emails;

public class SendPasswordResetEmailCommandHandler(IEmailService emailService) : ICommandHandler<SendPasswordResetEmailCommand>
{
    public async Task<Result> Handle(SendPasswordResetEmailCommand command, CancellationToken cancellationToken)
    {
        await emailService.SendAsync(
            toEmail: command.ToEmail,
            toName: command.ToName,
            subject: "Redefinição de senha — Societiza",
            body: BuildBody(command.ToName, command.ResetLink),
            cancellationToken: cancellationToken);

        return Result.Success();
    }

    private static string BuildBody(string name, string resetLink) => $"""
        <p>Olá, {name}!</p>
        <p>Recebemos uma solicitação para redefinir a senha da sua conta no <strong>Societiza</strong>.</p>
        <p>Clique no link abaixo para criar uma nova senha. O link é válido por <strong>1 hora</strong>.</p>
        <p><a href="{resetLink}">Redefinir senha</a></p>
        <p>Se você não solicitou a redefinição, ignore este email — sua senha permanece a mesma.</p>
        """;
}
```

---

## Referência — SendPasswordResetEmailCommand

O command já existe em `Identity.Shared/Emails/` com a seguinte estrutura esperada:

```csharp
namespace Identity.Shared.Emails;

public sealed record SendPasswordResetEmailCommand(
    string ToEmail,
    string ToName,
    string ResetLink
);
```

> Todos os campos são primitivos — garantia de serialização pelo Hangfire.

---

*Gerado em 02/04/2026.*