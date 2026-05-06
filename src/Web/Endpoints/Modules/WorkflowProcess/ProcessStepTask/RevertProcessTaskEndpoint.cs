using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowProcess.ProcessStepTask.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowProcess.Shared.Commands.ProcessStepTask;

namespace Web.Endpoints.Modules.WorkflowProcess.ProcessStepTask;

[Authorize]
public sealed class RevertProcessTaskEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<RevertProcessTaskRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowProcessRoutes.RevertProcessTask)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "RevertProcessTask",
        Summary     = "Desfazer conclusão de tarefa do processo",
        Description = """
            Reverte uma tarefa concluída ou pulada de volta ao status `Pending`.

            **Regras:**
            - A tarefa deve estar no status `Completed` ou `Skipped`.
            - Nenhuma etapa com ordem posterior à etapa da tarefa pode ter progresso (tarefas resolvidas ou status diferente de `NotStarted`).
            - Se a reversão fizer a etapa sair do status `Completed`, o status da etapa é recalculado automaticamente.
            - Se o processo estava `Completed`, ele retorna ao status `InProgress`.

            **Respostas:**
            - `200` - tarefa revertida com sucesso.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - processo, etapa ou tarefa não encontrados.
            - `409` - tarefa já está pendente ou etapa posterior possui progresso.

            **Efeitos:** Pode reverter o status da etapa e do processo se aplicável.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.WorkflowProcess.Tasks])]
    public override async Task<ActionResult> HandleAsync(
        RevertProcessTaskRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new RevertProcessTaskCommand(
                ProcessId: r.ProcessId,
                StepInstanceId: r.StepInstanceId,
                TaskInstanceId: r.TaskInstanceId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}