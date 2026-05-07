using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Domain.Pagination.Interfaces;
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
public sealed class ListProcessesByStepEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<ListProcessesByStepRequest>
    .WithActionResult<IPagedResult<object>>
{
    [ApiVersion("1.0")]
    [HttpGet(WorkflowProcessRoutes.ListProcessesByStep)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "ListProcessesByStep",
        Summary     = "Listar processos por etapa",
        Description = """
            Retorna a lista paginada de etapas do workflow, cada uma contendo todos os processos societários que possuem uma instância naquela etapa. Alimenta a visualização em board (Kanban).

            Cada página contém N grupos de etapas ordenados por posição crescente. Dentro de cada grupo, os processos são ordenados por data de criação decrescente.

            **Regras:**
            - `accountancyId` é obrigatório.
            - SystemAdmin pode consultar qualquer contabilidade. AccountancyAdmin e AccountancyEmployee só podem consultar a própria contabilidade.
            - `pageNumber` deve ser maior ou igual a 1 e `pageSize` deve estar entre 1 e 100.

            **Valores de enumeração na resposta:**
            - `processType`: `Abertura` | `Alteracao` | `Baixa`
            - `processStatus`: `InProgress` | `Completed`
            - `stepStatus`: `NotStarted` | `InProgress` | `Completed`

            **Respostas:**
            - `200` - página de grupos de etapas; pode ser vazia.
            - `400` - dados inválidos ou ausentes.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.WorkflowProcess.Processes])]
    public override async Task<ActionResult<IPagedResult<object>>> HandleAsync(
        ListProcessesByStepRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new ListProcessesByStepQuery(
                AccountancyId: r.AccountancyId,
                PageNumber:    r.PageNumber,
                PageSize:      r.PageSize))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
