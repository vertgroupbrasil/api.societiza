using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using Identity.Application.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Fields.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Commands.TemplateStepTask.Fields;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Fields;

public sealed class RemoveTemplateStepTaskFieldEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RemoveTemplateStepTaskFieldRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpDelete(WorkflowTemplateRoutes.RemoveTemplateStepTaskField)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "RemoveTemplateStepTaskField",
        Summary     = "Remover campo da tarefa",
        Description = """
            Remove um campo de uma tarefa de uma etapa do template.

            **Regras:**
            - Retorna 404 caso o template, a etapa, a tarefa ou o campo não existam.

            **Efeitos:** Campo removido permanentemente da tarefa.

            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.StepTaskFields])]
    public override async Task<ActionResult> HandleAsync(RemoveTemplateStepTaskFieldRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new RemoveTemplateStepTaskFieldCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                TaskId: request.TaskId,
                FieldId: request.FieldId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
