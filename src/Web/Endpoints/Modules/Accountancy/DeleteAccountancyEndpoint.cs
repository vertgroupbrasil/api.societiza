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
public sealed class DeleteAccountancyEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<DeleteAccountancyRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpDelete(AccountancyRoutes.DeleteAccountancy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "DeleteAccountancy",
        Summary     = "Excluir contabilidade",
        Description = """
            Realiza a exclusão lógica (soft delete) de uma contabilidade. Os dados são preservados mas a contabilidade deixa de aparecer em listagens.

            **Regras:**
            - Retorna 404 caso a contabilidade não exista.

            **Efeitos:** A contabilidade é marcada como excluída (soft delete) e não é retornada em listagens.

            **Autorização:** SystemAdmin.
            """,
        Tags = [Tags.Accountancy])]
    public override async Task<ActionResult> HandleAsync(DeleteAccountancyRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new DeleteAccountancyCommand(request.Id))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}