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

public sealed class UpdateWorkflowTemplateStepTaskEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<UpdateWorkflowTemplateStepTaskRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [Authorize(Policy = IdentityPolicies.CanManageWorkflowTemplates)]
    [HttpPut(WorkflowTemplateRoutes.UpdateWorkflowTemplateStepTask)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "UpdateWorkflowTemplateStepTask",
        Summary     = "Atualizar tarefa da etapa",
        Description = """
            Atualiza os dados de uma tarefa em uma etapa do template.

            **Regras:**
            - Retorna 404 caso o template, a etapa ou a tarefa não existam.
            - Valores aceitos para `type`:
              - `Manual` — tarefa concluída manualmente por um usuário.
              - `Automatic` — tarefa executada automaticamente pelo sistema.
              - `Approval` — tarefa que requer aprovação explícita.
            - Se a `order` solicitada já estiver ocupada, as tarefas existentes são deslocadas para frente automaticamente.
            **Efeitos:** Dados da tarefa atualizados, possivelmente reordenando tarefas existentes.
            **Autorização:** Requer autenticação. Papel: SystemAdmin.
            """,
        Tags = [Tags.WorkflowTemplate.StepTasks])]
    public override async Task<ActionResult> HandleAsync(UpdateWorkflowTemplateStepTaskRequest request, CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new UpdateWorkflowTemplateStepTaskCommand(
                TemplateId: request.TemplateId,
                StepId: request.StepId,
                TaskId: request.TaskId,
                Title: request.Body.Title,
                Description: request.Body.Description,
                Type: request.Body.Type,
                Configuration: request.Body.Configuration,
                Order: request.Body.Order,
                IsOptional: request.Body.IsOptional))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
