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

public sealed class ResendInviteEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ResendInviteRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.ResendInvite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        Summary = "Reenviar convite",
        Description = "Gera um novo token e reenvia o e-mail de convite. Apenas convites com status Pending podem ser reenviados.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult> HandleAsync(ResendInviteRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(request => new ResendInviteCommand(
                InviteId: request.InviteId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
