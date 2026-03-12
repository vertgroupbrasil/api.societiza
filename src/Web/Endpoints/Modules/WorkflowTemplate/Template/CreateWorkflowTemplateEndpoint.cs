using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Response;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.Template;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template;

public sealed class CreateWorkflowTemplateEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<CreateWorkflowTemplateRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowTemplateRoutes.CreateWorkflowTemplate)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
            Summary = "Criar um novo template de workflow",
            Description = "Cria um novo template de workflow com status Draft. Retorna o identificador (GUID) do template criado.",
            Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(CreateWorkflowTemplateRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new CreateWorkflowTemplateCommand(
                Name: request.Name,
                Description: request.Description))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}