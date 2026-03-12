using Accountancy.Shared.Commands;
using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Response;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.Accountancy.Requests.Commands;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.Accountancy;

public sealed class CreateAccountancyEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<CreateAccountancyRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(AccountancyRoutes.CreateAccountancy)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
            Summary = "Criar uma nova contabilidade",
            Description = "Cadastra uma nova contabilidade no sistema. O CNPJ deve ser único — retorna 409 (Conflict) caso já exista uma contabilidade com o mesmo CNPJ. Em caso de sucesso, retorna o identificador (GUID) da contabilidade criada.",
            Tags = [Tags.Accountancy])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(CreateAccountancyRequest request, CancellationToken cancellationToken)
        => await Result.Create(request)
            .Map(request => new CreateAccountancyCommand(
                CNPJ: request.CNPJ,
                LegalName: request.LegalName,
                TradeName: request.TradeName,
                Address: request.Address,
                City: request.City,
                State: request.State,
                PostalCode: request.PostalCode,
                Phone: request.Phone,
                Email: request.Email))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}