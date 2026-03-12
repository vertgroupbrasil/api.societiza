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

public sealed class UpdateAccountancyEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateAccountancyRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPut(AccountancyRoutes.UpdateAccountancy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
            Summary = "Atualizar dados de uma contabilidade",
            Description = "Atualiza os dados cadastrais de uma contabilidade existente a partir do seu identificador único (GUID). Todos os campos editáveis devem ser enviados no body da requisição.",
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