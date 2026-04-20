using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Queries.Invitation;
using Identity.Shared.Response.Invitation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Invitation.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Invitation;

public sealed class GetInvitationLinkByTokenEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetInvitationLinkByTokenRequest>
    .WithActionResult<InvitationLinkPublicResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.GetInvitationLinkByToken)]
    [ProducesResponseType(typeof(InvitationLinkPublicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "GetInvitationLinkByToken",
        Summary     = "Obter link de convite por token",
        Description = """
            Retorna o status público de um link de convite a partir do token, permitindo ao frontend decidir se exibe o formulário de cadastro ou uma mensagem de erro.

            **Regras:**
            - Retorna 404 se o token não corresponder a nenhum link ativo.
            - O status retornado reflete a situação em tempo real — um link com Status Active mas ExpiresAt no passado é retornado como Expired.

            **Efeitos:** Nenhum.

            **Autorização:** Público.
            """,
        Tags = [Tags.Identity.Invites])]
    public override async Task<ActionResult<InvitationLinkPublicResponse>> HandleAsync(
        GetInvitationLinkByTokenRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new GetInvitationLinkByTokenQuery(r.Token))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}