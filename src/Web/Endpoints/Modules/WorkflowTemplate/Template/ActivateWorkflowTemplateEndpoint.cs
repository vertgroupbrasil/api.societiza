using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.Template;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template;

public sealed class ActivateWorkflowTemplateEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ActivateWorkflowTemplateRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPatch(WorkflowTemplateRoutes.ActivateWorkflowTemplate)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
            Summary = "Ativar um template de workflow",
            Description = "Altera o status de um template de workflow para Active, tornando-o disponível para uso.",
            Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult> HandleAsync(ActivateWorkflowTemplateRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new ActivateWorkflowTemplateCommand(request.Id))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}