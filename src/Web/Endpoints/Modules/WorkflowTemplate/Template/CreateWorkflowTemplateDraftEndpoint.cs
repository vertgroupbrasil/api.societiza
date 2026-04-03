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

public sealed class CreateWorkflowTemplateDraftEndpoint(ISender sender)
    : EndpointBaseAsync
        .WithRequest<CreateWorkflowTemplateDraftRequest>
        .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowTemplateRoutes.DraftWorkflowTemplate)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Criar rascunho derivado de um workflow",
        Description = "Clona o workflow atual para um novo rascunho editável.",
        Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(
        CreateWorkflowTemplateDraftRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(value => new CreateWorkflowTemplateDraftCommand(value.Id))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
