using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Shared.Commands.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Auth.Requests;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity.Auth;

public sealed class ResetPasswordEndpoint(ISender sender)
    : EndpointBaseAsync
        .WithRequest<ResetPasswordRequest>
        .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.ResetPassword)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Reset password",
        Description = "Consumes the password reset token and sets a new password.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult> HandleAsync(
        ResetPasswordRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new ResetPasswordCommand(Token: r.Token, NewPassword: r.NewPassword))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
