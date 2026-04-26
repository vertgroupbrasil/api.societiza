using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Queries.Template;
using WorkflowTemplate.Shared.Response.Template;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template;

public sealed class GetAllWorkflowTemplatesEndpoint(ISender sender) : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<WorkflowTemplateResponse>>
{
    [ApiVersion("1.0")]
    [Authorize]
    [HttpGet(WorkflowTemplateRoutes.GetAllWorkflowTemplates)]
    [ProducesResponseType(typeof(List<WorkflowTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [SwaggerOperation(
        OperationId = "GetAllWorkflowTemplates",
        Summary     = "Listar templates de workflow",
        Description = """
            Retorna uma lista resumida de todos os templates de workflow com `Id`, `Name` e `Description`.

            **Regras:** Nenhuma.

            **Respostas:**
            - `200` - lista de templates de workflow.
            - `400` - requisição inválida.
            - `401` - access token ausente ou inválido.

            **Efeitos:** Nenhum.

            **Autorização:** Qualquer usuário autenticado.
            """,
        Tags = [Tags.WorkflowTemplate.Templates])]
    public override async Task<ActionResult<List<WorkflowTemplateResponse>>> HandleAsync(CancellationToken cancellationToken = default)
        => await Result.Create(new GetAllWorkflowTemplatesQuery())
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
