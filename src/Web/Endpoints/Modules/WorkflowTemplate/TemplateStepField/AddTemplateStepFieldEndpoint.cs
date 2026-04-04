using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Response;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStepField.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStepField;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepField;

public sealed class AddTemplateStepFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<AddTemplateStepFieldRequest>
    .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowTemplateRoutes.AddTemplateStepField)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Adicionar um campo a uma etapa do template",
            Description = """
                Adds a field to a step.

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
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(AddTemplateStepFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new AddTemplateStepFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                Label: request.Body.Label,
                FieldType: request.Body.FieldType,
                Options: request.Body.Options,
                Order: request.Body.Order))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
