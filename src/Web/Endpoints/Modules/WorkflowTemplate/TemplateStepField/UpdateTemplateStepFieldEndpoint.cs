using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStepField.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStepField;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepField;

public sealed class UpdateTemplateStepFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateTemplateStepFieldRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPut(WorkflowTemplateRoutes.UpdateTemplateStepField)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
            Summary = "Atualizar um campo de uma etapa do template",
            Description = "Atualiza o label, tipo e opções de um campo (field) existente em uma etapa (step) do template de workflow.",
            Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult> HandleAsync(UpdateTemplateStepFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new UpdateTemplateStepFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                FieldId: request.FieldId,
                Label: request.Body.Label,
                FieldType: request.Body.FieldType,
                Options: request.Body.Options))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}