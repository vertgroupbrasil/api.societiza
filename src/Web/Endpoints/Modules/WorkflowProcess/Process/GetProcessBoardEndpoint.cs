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
public sealed class GetProcessBoardEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetProcessBoardRequest>
    .WithActionResult<List<ProcessBoardItemResponse>>
{
    [ApiVersion("1.0")]
    [HttpGet(WorkflowProcessRoutes.GetProcessBoard)]
    [ProducesResponseType(typeof(List<ProcessBoardItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "GetProcessBoard",
        Summary     = "Listar processos societários",
        Description = """
            Retorna a lista de processos societários de uma contabilidade para exibição em painel (board).

            **Regras:**
            - `accountancyId` é obrigatório.
            - `processType` é opcional; quando informado, filtra por tipo de processo.

            **Valores de enumeração na requisição:**
            - `processType`: `Abertura` | `Alteracao` | `Baixa`

            **Valores de enumeração na resposta:**
            - `processType`: `Abertura` | `Alteracao` | `Baixa`
            - `status`: `InProgress` | `Completed`

            **Respostas:**
            - `200` - lista de processos; pode ser vazia.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.WorkflowProcess.Processes])]
    public override async Task<ActionResult<List<ProcessBoardItemResponse>>> HandleAsync(
        GetProcessBoardRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new GetProcessBoardQuery(
                AccountancyId: r.AccountancyId,
                ProcessType: r.ProcessType))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
