using Accountancy.Shared.Queries;
using Accountancy.Shared.Response;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Accountancy.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy;

[Authorize(Policy = IdentityPolicies.CanManageAccountancies)]
public sealed class GetAccountancyByIdEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetAccountancyByIdRequest>
    .WithActionResult<AccountancyDetailResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(AccountancyRoutes.GetAccountancyById)]
    [ProducesResponseType(typeof(AccountancyDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "GetAccountancyById",
        Summary     = "Obter contabilidade por ID",
        Description = """
            Retorna os dados completos de uma contabilidade a partir do seu identificador único.

            **Regras:**
            - Exclusivo para SystemAdmin; outros perfis devem usar `GET /accountancies/me`.

            **Respostas:**
            - `200` - contabilidade encontrada; retorna os dados completos.
            - `400` - identificador inválido ou ausente.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - contabilidade não encontrada.

            **Efeitos:** Nenhum.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.Accountancy])]
    public override async Task<ActionResult<AccountancyDetailResponse>> HandleAsync(
        GetAccountancyByIdRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new GetAccountancyDetailByIdQuery(r.Id))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}