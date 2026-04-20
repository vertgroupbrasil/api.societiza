using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStep.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStep;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStep;

public sealed class RemoveWorkflowTemplateStepEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RemoveWorkflowTemplateStepRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpDelete(WorkflowTemplateRoutes.RemoveWorkflowTemplateStep)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "RemoveWorkflowTemplateStep",
        Summary     = "Remover etapa do template",
        Description = """
            Remove uma etapa do template, junto com todas as suas tarefas e campos.

            **Regras:**
            - Retorna 404 caso o template ou a etapa não existam.

            **Efeitos:** Etapa e todas as tarefas e campos aninhados são removidos permanentemente.

            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.Steps])]
    public override async Task<ActionResult> HandleAsync(RemoveWorkflowTemplateStepRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new RemoveWorkflowTemplateStepCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
