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
            Description = "Adiciona um novo campo (field) a uma etapa (step) do template de workflow especificado. Retorna o identificador (GUID) do campo criado.",
            Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(AddTemplateStepFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new AddTemplateStepFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                Label: request.Body.Label,
                FieldType: request.Body.FieldType,
                Options: request.Body.Options))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}