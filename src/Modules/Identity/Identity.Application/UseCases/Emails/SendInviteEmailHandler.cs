using Core.Application.Messaging.Commands;
using Core.Application.Services.Interfaces;
using Core.Shared.Results;
using Identity.Shared.Emails;

namespace Identity.Application.UseCases.Emails;

public class SendInviteEmailHandler(IEmailService emailService) : ICommandHandler<SendInviteEmailCommand>
{
    public async Task<Result> Handle(SendInviteEmailCommand command, CancellationToken cancellationToken)
    {
        await emailService.SendAsync(
            toEmail: command.ToEmail,
            toName: command.ToName,
            subject: "Você foi convidado para o Societiza",
            body: BuildBody(command.ToName, command.InviteLink),
            cancellationToken: cancellationToken);

        return Result.Success();
    }

    private static string BuildBody(string name, string inviteLink) => $"""
        <p>Olá, {name}!</p>
        <p>Você foi convidado para acessar a plataforma <strong>Societiza</strong>.</p>
        <p>Clique no link abaixo para aceitar o convite e definir sua senha:</p>
        <p><a href="{inviteLink}">Aceitar convite</a></p>
        """;
}