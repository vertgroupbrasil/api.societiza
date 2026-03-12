using Accountancy.Shared.Commands;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Accountancy.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy;

public sealed class DeleteAccountancyEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<DeleteAccountancyRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpDelete(AccountancyRoutes.DeleteAccountancy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
            Summary = "Excluir uma contabilidade",
            Description = "Realiza a exclusão lógica (soft delete) de uma contabilidade, marcando-a como excluída. A contabilidade não será mais retornada nas listagens, mas seus dados são preservados no sistema.",
            Tags = [Tags.Accountancy])]
    public override async Task<ActionResult> HandleAsync(DeleteAccountancyRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new DeleteAccountancyCommand(request.Id))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}