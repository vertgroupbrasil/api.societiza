using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using Identity.Shared.Commands.Invitation;
using Identity.Shared.Response.Invitation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Invitation.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Invitation;

[Authorize(Policy = IdentityPolicies.CanManageInvites)]
public sealed class CreateInvitationLinkEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<CreateInvitationLinkRequest>
    .WithActionResult<CreateInvitationLinkResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.CreateInvitationLink)]
    [ProducesResponseType(typeof(CreateInvitationLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "CreateInvitationLink",
        Summary     = "Criar link de convite",
        Description = """
            Cria um link de convite reutilizável com validade de 7 dias e retorna o token gerado.

            **Regras:**
            - Apenas um link ativo por escopo (tipo + contabilidade) é permitido — retorna 409 se já existir um ativo.
            - SystemAdmin pode criar links para SystemAdmin e AccountancyAdmin.
            - AccountancyAdmin pode criar links para AccountancyAdmin e AccountancyEmployee.
            - `MaxUses` é obrigatório apenas para links com TargetRole AccountancyEmployee; para os demais é fixado em 1.
            - `AccountancyId` deve ser nulo quando o convite for SA→SA; obrigatório quando for SA→AA; ignorado quando o criador for AccountancyAdmin (o sistema utiliza a contabilidade do usuário autenticado).

            **Respostas:**
            - `200` — link criado; retorna o identificador e o token gerado.
            - `400` — dados inválidos ou ausentes.
            - `401` — access token ausente ou inválido.
            - `403` — usuário sem permissão (requer SystemAdmin ou AccountancyAdmin).
            - `409` — já existe um link ativo para o mesmo escopo (tipo + contabilidade).
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult<CreateInvitationLinkResponse>> HandleAsync(CreateInvitationLinkRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new CreateInvitationLinkCommand(r.TargetRole, r.AccountancyId, r.MaxUses))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}