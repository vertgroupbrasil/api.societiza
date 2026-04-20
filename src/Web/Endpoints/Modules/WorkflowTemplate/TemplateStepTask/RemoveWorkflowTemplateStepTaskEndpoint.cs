using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpDelete(WorkflowTemplateRoutes.RemoveWorkflowTemplateStepTask)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "RemoveWorkflowTemplateStepTask",
        Summary     = "Remover tarefa da etapa",
        Description = """
            Remove uma tarefa de uma etapa do template, junto com todos os seus campos.

            **Regras:**
            - Retorna 404 caso o template, a etapa ou a tarefa não existam.

            **Efeitos:** Tarefa e todos os campos aninhados são removidos permanentemente.

            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.StepTasks])]
    public override async Task<ActionResult> HandleAsync(RemoveWorkflowTemplateStepTaskRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new RemoveWorkflowTemplateStepTaskCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                TaskId: request.TaskId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
