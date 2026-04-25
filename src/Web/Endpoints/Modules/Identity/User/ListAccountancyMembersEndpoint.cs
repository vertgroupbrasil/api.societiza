using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Queries.User;
using Identity.Shared.Response.User;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.User.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.User;

[Authorize]
public sealed class ListAccountancyMembersEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ListAccountancyMembersRequest>
    .WithActionResult<AccountancyMembersResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(IdentityRoutes.ListAccountancyMembers)]
    [ProducesResponseType(typeof(AccountancyMembersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "ListAccountancyMembers",
        Summary     = "Listar membros da contabilidade",
        Description = """
            Retorna os membros ativos de uma contabilidade. O nível de detalhe varia conforme o perfil do solicitante.

            **Regras:**
            - AccountancyAdmin e SystemAdmin recebem dados completos (e-mail, data de entrada, link de convite).
            - AccountancyEmployee recebe apenas nome e perfil dos colegas.
            - AccountancyAdmin e AccountancyEmployee só podem listar membros da própria contabilidade.
            - SystemAdmin pode listar membros de qualquer contabilidade.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado (dados filtrados por perfil).
            """,
        Tags = [Tags.Identity.Users])]
    public override async Task<ActionResult<AccountancyMembersResponse>> HandleAsync(
        ListAccountancyMembersRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new ListAccountancyMembersQuery(r.AccountancyId))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
