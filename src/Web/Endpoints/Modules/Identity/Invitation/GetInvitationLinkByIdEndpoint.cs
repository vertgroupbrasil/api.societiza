using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using Identity.Shared.Queries.Invitation;
using Identity.Shared.Response.Invitation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Invitation.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Invitation;

[Authorize(Policy = IdentityPolicies.CanManageInvites)]
public sealed class GetInvitationLinkByIdEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetInvitationLinkByIdRequest>
    .WithActionResult<InvitationLinkByIdResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.GetInvitationLinkById)]
    [ProducesResponseType(typeof(InvitationLinkByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "GetInvitationLinkById",
        Summary     = "Obter link de convite por ID",
        Description = """
            Retorna os dados de um link de convite específico e a lista de usuários que se cadastraram através dele.

            **Regras:**
            - AccountancyAdmin só pode consultar links do próprio tenant.
            - SystemAdmin tem acesso a qualquer link.

            **Respostas:**
            - `200` — link encontrado; retorna os dados completos e os usuários registrados pelo link.
            - `400` — identificador inválido ou ausente.
            - `401` — access token ausente ou inválido.
            - `403` — usuário sem permissão (requer SystemAdmin ou AccountancyAdmin).
            - `404` — link de convite não encontrado.
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult<InvitationLinkByIdResponse>> HandleAsync(GetInvitationLinkByIdRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new GetInvitationLinkByIdQuery(r.InvitationLinkId))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}