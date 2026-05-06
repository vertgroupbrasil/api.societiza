using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowProcess.Process.Requests.Queries;
using Web.Endpoints.Routes;
using WorkflowProcess.Shared.Queries;
using WorkflowProcess.Shared.Response.Process;

namespace Web.Endpoints.Modules.WorkflowProcess.Process;

[Authorize]
public sealed class GetProcessDetailEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetProcessDetailRequest>
    .WithActionResult<ProcessDetailResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(WorkflowProcessRoutes.GetProcessDetail)]
    [ProducesResponseType(typeof(ProcessDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "GetProcessDetail",
        Summary     = "Obter detalhes do processo societário",
        Description = """
            Retorna os detalhes completos de um processo societário, incluindo etapas, tarefas e campos preenchidos.

            **Regras:** Nenhuma.

            **Valores de enumeração na resposta:**
            - `processType`: `Abertura` | `Alteracao` | `Baixa`
            - `status` (process): `InProgress` | `Completed`
            - `status` (step): `NotStarted` | `InProgress` | `Completed`
            - `status` (task): `Pending` | `Completed` | `Skipped`
            - `fieldType` (campos de step e de task): `Text` | `Boolean` | `Integer` | `Select` | `Date`

            **Respostas:**
            - `200` - processo encontrado; retorna detalhes completos com etapas e tarefas.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.
            - `404` - processo não encontrado.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.WorkflowProcess.Processes])]
    public override async Task<ActionResult<ProcessDetailResponse>> HandleAsync(
        GetProcessDetailRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new GetProcessDetailQuery(ProcessId: r.ProcessId))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
