using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStep.Fields.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStep.Fields;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStep.Fields;

public sealed class RemoveTemplateStepFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RemoveTemplateStepFieldRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpDelete(WorkflowTemplateRoutes.RemoveTemplateStepField)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "RemoveTemplateStepField",
        Summary     = "Remover campo da etapa",
        Description = """
            Remove um campo de uma etapa do template.

            **Regras:** Retorna 404 caso o template, a etapa ou o campo não existam.
            **Efeitos:** Campo removido permanentemente da etapa.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.StepFields])]
    public override async Task<ActionResult> HandleAsync(RemoveTemplateStepFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new RemoveTemplateStepFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                FieldId: request.FieldId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
