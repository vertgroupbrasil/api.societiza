using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Domain.Pagination.Interfaces;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Queries;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Queries.Template;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template;

[Authorize]
public sealed class GetAllWorkflowTemplatesEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetAllWorkflowTemplatesRequest>
    .WithActionResult<IPagedResult<object>>
{
    [ApiVersion("1.0")]
    [HttpGet(WorkflowTemplateRoutes.GetAllWorkflowTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        OperationId = "GetAllWorkflowTemplates",
        Summary     = "Listar templates de workflow",
        Description = """
            Retorna a lista paginada de templates de workflow.

            **Regras:**
            - `AccountancyAdmin` e `AccountancyEmployee` visualizam apenas templates `Active`.
            - `SystemAdmin` visualiza `Active` por padrão; use `includeDraft=true` e/ou `includeArchive=true` para incluir outros status.
            - `pageNumber` deve ser maior ou igual a 1.
            - `pageSize` deve estar entre 1 e 100.

            **Valores de enumeração na resposta:**
            - `status`: `Draft` | `Active` | `Archived`

            **Respostas:**
            - `200` - página de resultados paginados.
            - `400` - parâmetros de paginação inválidos.
            - `401` - access token ausente ou inválido.
            - `403` - usuário sem permissão.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.WorkflowTemplate.Templates])]
    public override async Task<ActionResult<IPagedResult<object>>> HandleAsync(
        GetAllWorkflowTemplatesRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(r => new GetAllWorkflowTemplatesQuery(
                IncludeDraft:   r.IncludeDraft,
                IncludeArchive: r.IncludeArchive,
                PageNumber:     r.PageNumber,
                PageSize:       r.PageSize))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
