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

public sealed class PublishWorkflowTemplateEndpoint(ISender sender)
    : EndpointBaseAsync
        .WithRequest<PublishWorkflowTemplateRequest>
        .WithActionResult<IdentifierResponse>
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowTemplateRoutes.PublishWorkflowTemplate)]
    [ProducesResponseType(typeof(IdentifierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Publicar um workflow em rascunho",
        Description = "Aplica as alterações do rascunho ao workflow em uso e retorna o identificador do workflow ativo.",
        Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult<IdentifierResponse>> HandleAsync(
        PublishWorkflowTemplateRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(value => new PublishWorkflowTemplateCommand(value.Id))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
