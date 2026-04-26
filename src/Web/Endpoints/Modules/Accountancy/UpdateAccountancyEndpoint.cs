using Accountancy.Shared.Commands;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Accountancy.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy;

[Authorize(Policy = IdentityPolicies.CanManageAccountancies)]
public sealed class UpdateAccountancyEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateAccountancyRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPut(AccountancyRoutes.UpdateAccountancy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "UpdateAccountancy",
        Summary     = "Atualizar contabilidade",
        Description = """
            Atualiza os dados cadastrais de uma contabilidade existente.

            **Regras:**
            - Todos os campos editáveis são obrigatórios no corpo.

            **Respostas:**
            - `200` - dados atualizados com sucesso.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - contabilidade não encontrada.

            **Efeitos:** Dados cadastrais da contabilidade atualizados.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.Accountancy])]
    public override async Task<ActionResult> HandleAsync(UpdateAccountancyRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new UpdateAccountancyCommand(
                Id: request.Id,
                CNPJ: request.Body.CNPJ,
                LegalName: request.Body.LegalName,
                TradeName: request.Body.TradeName,
                Address: request.Body.Address,
                City: request.Body.City,
                State: request.Body.State,
                PostalCode: request.Body.PostalCode,
                Phone: request.Body.Phone,
                Email: request.Body.Email))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}