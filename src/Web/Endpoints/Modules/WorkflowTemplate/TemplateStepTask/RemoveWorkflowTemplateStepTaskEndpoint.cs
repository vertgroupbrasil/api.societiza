using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask;

public sealed class RemoveWorkflowTemplateStepTaskEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RemoveWorkflowTemplateStepTaskRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpDelete(WorkflowTemplateRoutes.RemoveWorkflowTemplateStepTask)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
            Summary = "Remover uma tarefa de uma etapa do template",
            Description = "Remove uma tarefa (task) existente de uma etapa (step) do template de workflow especificado.",
            Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult> HandleAsync(RemoveWorkflowTemplateStepTaskRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new RemoveWorkflowTemplateStepTaskCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                TaskId: request.TaskId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}