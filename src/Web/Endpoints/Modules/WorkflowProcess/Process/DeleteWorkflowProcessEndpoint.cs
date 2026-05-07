using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowProcess.Process.Requests.Commands;
using Web.Endpoints.Routes;
using WorkflowProcess.Shared.Commands.Process;

namespace Web.Endpoints.Modules.WorkflowProcess.Process;

[Authorize]
public sealed class DeleteWorkflowProcessEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<DeleteWorkflowProcessRequest>
    .WithActionResult
{
    [ApiVersion("1.0")]
    [HttpDelete(WorkflowProcessRoutes.DeleteWorkflowProcess)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "DeleteWorkflowProcess",
        Summary     = "Excluir processo societário",
        Description = """
            Exclui (soft delete) um processo societário pelo identificador.

            **Regras:**
            - Apenas o SystemAdmin ou membros da contabilidade dona do processo podem excluí-lo.

            **Respostas:**
            - `204` - processo excluído com sucesso.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - processo não encontrado.
            """,
        Tags = [Tags.WorkflowProcess.Processes])]
    public override async Task<ActionResult> HandleAsync(
        DeleteWorkflowProcessRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new DeleteWorkflowProcessCommand(r.ProcessId))
            .Bind(command => sender.Send(command, cancellationToken))
            .Match(NoContent, this.HandleFailure);
}
