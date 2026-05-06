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
public sealed class SkipProcessTaskEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<SkipProcessTaskRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpPost(WorkflowProcessRoutes.SkipProcessTask)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [SwaggerOperation(
        OperationId = "SkipProcessTask",
        Summary     = "Pular tarefa do processo",
        Description = """
            Marca uma tarefa opcional como pulada dentro de uma etapa do processo societário.

            **Regras:**
            - O processo deve estar no status `InProgress`.
            - A etapa informada deve ser a etapa atual (a primeira etapa ainda não concluída).
            - Todas as tarefas obrigatórias com ordem inferior devem estar resolvidas antes desta.
            - A tarefa deve estar no status `Pending`.
            - Quando todas as tarefas obrigatórias da etapa forem resolvidas, a etapa é concluída automaticamente.

            **Respostas:**
            - `200` - tarefa pulada com sucesso.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - processo, etapa ou tarefa não encontrados.
            - `409` - etapa bloqueada, tarefa anterior obrigatória pendente, processo já concluído ou tarefa já resolvida.

            **Efeitos:** Pode concluir a etapa automaticamente se todos os requisitos forem satisfeitos.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.WorkflowProcess.Tasks])]
    public override async Task<ActionResult> HandleAsync(
        SkipProcessTaskRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new SkipProcessTaskCommand(
                ProcessId: r.ProcessId,
                StepInstanceId: r.StepInstanceId,
                TaskInstanceId: r.TaskInstanceId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(() => Ok(), this.HandleFailure);
}