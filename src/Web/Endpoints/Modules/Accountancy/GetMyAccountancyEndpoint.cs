using Accountancy.Shared.Queries;
using Accountancy.Shared.Response;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Application.Services.Interfaces;
using Core.Endpoints.Extensions;
using Core.Shared.Errors;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy;

[Authorize]
public sealed class GetMyAccountancyEndpoint(ISender sender, ICurrentUserService currentUser) : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<AccountancyDetailResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(AccountancyRoutes.GetMyAccountancy)]
    [ProducesResponseType(typeof(AccountancyDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "GetMyAccountancy",
        Summary     = "Obter minha contabilidade",
        Description = """
            Retorna os dados completos da contabilidade associada ao usuário autenticado, a partir da claim `accountancy_id`. Destinado a AccountancyAdmin e AccountancyEmployee.

            **Respostas:**
            - `200` — contabilidade encontrada; retorna os dados completos.
            - `401` — access token ausente ou inválido.
            - `403` — usuário não possui a claim `accountancy_id` (ex.: SystemAdmin sem vínculo a uma contabilidade).
            - `404` — contabilidade não encontrada.
            """,
        Tags = [Tags.Accountancy])]
    public override async Task<ActionResult<AccountancyDetailResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.AccountancyId is not Guid id)
            return this.HandleFailure(Result.Failure<AccountancyDetailResponse>(
                new ForbiddenError(new Error("AccountancyClaimMissing", "User does not have an associated accountancy."))));

        return await Result.Create(id)
            .Map(accountancyId => new GetAccountancyDetailByIdQuery(accountancyId))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
    }
}