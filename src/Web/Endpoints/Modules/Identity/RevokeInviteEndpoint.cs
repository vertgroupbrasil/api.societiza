using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity;

public sealed class RevokeInviteEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RevokeInviteRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.RevokeInvite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        Summary = "Revogar convite",
        Description = "Revoga um convite pendente. Apenas convites com status Pending podem ser revogados.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult> HandleAsync(RevokeInviteRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(request => new RevokeInviteCommand(
                InviteId: request.InviteId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
