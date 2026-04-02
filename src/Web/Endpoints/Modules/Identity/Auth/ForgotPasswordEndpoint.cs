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

public sealed class ForgotPasswordEndpoint(ISender sender)
    : EndpointBaseAsync
        .WithRequest<ForgotPasswordRequest>
        .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.ForgotPassword)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        Summary = "Forgot password",
        Description = "Sends a password reset email if the account exists. Always returns 200 to prevent email enumeration.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult> HandleAsync(
        ForgotPasswordRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(r => new ForgotPasswordCommand(Email: r.Email))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}
