using Accountancy.Shared.Queries;
using Accountancy.Shared.Response;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Application.Services.Interfaces;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Accountancy.Requests.Queries;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy;

[Authorize]
public sealed class GetAccountancyByIdEndpoint(
    ISender sender,
    ICurrentUserService currentUser) : EndpointBaseAsync
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
            - Retorna 404 caso a contabilidade não exista.

            **Efeitos:** Nenhum.

            **Autorização:** SystemAdmin (acessa qualquer contabilidade). Demais roles só acessam a própria contabilidade (claim `accountancy_id`); tentativa em outra contabilidade resulta em 403.
            """,
        Tags = [Tags.Accountancy])]
    public override async Task<ActionResult<AccountancyDetailResponse>> HandleAsync(
        GetAccountancyByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        currentUser.ResolveAccountancyId(request.Id);

        return await Result.Create(request)
            .Map(request => new GetAccountancyDetailByIdQuery(request.Id))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
    }
}