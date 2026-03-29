using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Response;
using Core.Shared.Results;
using Identity.Shared.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Identity.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Identity;

public sealed class InviteUserEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<InviteUserRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(IdentityRoutes.InviteUser)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        Summary = "Convidar um novo usuário",
        Description = "Envia um convite para um novo usuário acessar a plataforma. O e-mail deve ser único — retorna 409 (Conflict) caso já exista um convite pendente ou usuário com o mesmo e-mail. Em caso de sucesso, retorna o identificador (GUID) do convite criado.",
        Tags = [Tags.Identity])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(InviteUserRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(request => new InviteUserCommand(
                Email: request.Email,
                Role: request.Role,
                AccountancyId: request.AccountancyId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}