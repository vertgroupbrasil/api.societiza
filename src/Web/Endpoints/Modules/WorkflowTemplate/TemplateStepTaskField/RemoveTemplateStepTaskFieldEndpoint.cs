using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTaskField.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTaskField;

public sealed class RemoveTemplateStepTaskFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RemoveTemplateStepTaskFieldRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpDelete(WorkflowTemplateRoutes.RemoveTemplateStepTaskField)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
            Summary = "Remover um campo de uma tarefa de uma etapa do template",
            Description = "Remove um campo (field) existente de uma tarefa (task) de uma etapa (step) do template de workflow especificado.",
            Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult> HandleAsync(RemoveTemplateStepTaskFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new RemoveTemplateStepTaskFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                TaskId: request.TaskId,
                FieldId: request.FieldId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}