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
