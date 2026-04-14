using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Queries;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Queries.Template;
using WorkflowTemplate.Shared.Response.Template;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template;

public sealed class GetWorkflowTemplateByIdEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetWorkflowTemplateByIdRequest>
    .WithActionResult<WorkflowTemplateDetailResponse>
{
    [ApiVersion("1.0")]
    [Authorize]
    [HttpGet(WorkflowTemplateRoutes.GetWorkflowTemplateById)]
    [ProducesResponseType(typeof(WorkflowTemplateDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "GetWorkflowTemplateById",
        Summary     = "Obter template de workflow por ID",
        Description = """
            Retorna os dados completos de um template de workflow a partir do seu identificador único.

            **Regras:** Retorna 404 caso o template não exista.
            **Efeitos:** Nenhum.
            **Autorização:** Requer autenticação. Papéis: SystemAdmin, AccountingAdmin, AccountingEmployee.
            """,
        Tags = [Tags.WorkflowTemplate.Templates])]
    public override async Task<ActionResult<WorkflowTemplateDetailResponse>> HandleAsync(
        GetWorkflowTemplateByIdRequest request,
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new GetWorkflowTemplateDetailByIdQuery(request.Id))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}
