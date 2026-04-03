using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Response;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTaskField.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTaskField;

public sealed class AddTemplateStepTaskFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AddTemplateStepTaskFieldRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowTemplateRoutes.AddTemplateStepTaskField)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Adicionar um campo a uma tarefa de uma etapa do template",
            Description = """
                Adds a field to a task.

                **fieldType** accepted values:
                - `Text` - single-line text input
                - `Boolean` - true/false toggle
                - `Integer` - numeric input
                - `Select` - dropdown with predefined options
                - `Date` - date picker

                `options` is required when `fieldType` is `Select`.
                `options` must be null for all other field types.
                """,
            Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(AddTemplateStepTaskFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new AddTemplateStepTaskFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                TaskId: request.TaskId,
                Label: request.Body.Label,
                FieldType: request.Body.FieldType,
                Options: request.Body.Options))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
