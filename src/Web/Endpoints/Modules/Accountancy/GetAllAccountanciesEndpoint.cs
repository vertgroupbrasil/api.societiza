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
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy;

[Authorize(Policy = IdentityPolicies.CanManageAccountancies)]
public sealed class GetAllAccountanciesEndpoint(ISender sender) : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<AccountancyDetailResponse>>
{
    [ApiVersion("1.0")]
    [HttpGet(AccountancyRoutes.GetAllAccountancies)]
    [ProducesResponseType(typeof(List<AccountancyDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "GetAllAccountancies",
        Summary     = "Listar contabilidades",
        Description = """
            Retorna a lista completa de contabilidades ativas, ordenadas alfabeticamente pela Razão Social (`LegalName`) e, em seguida, pela data de criação (`CreatedAt`).

            **Regras:**
            - Contabilidades excluídas (soft delete) não são retornadas.

            **Efeitos:** Nenhum.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.Accountancy])]
    public override async Task<ActionResult<List<AccountancyDetailResponse>>> HandleAsync(CancellationToken cancellationToken = default)
        => await Result.Create(new GetAllAccountanciesQuery())
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}