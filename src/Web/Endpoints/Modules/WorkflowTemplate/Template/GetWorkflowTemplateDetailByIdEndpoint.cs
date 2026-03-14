using Ardalis.ApiEndpoints;
using Asp.Versioning;
using Core.Endpoints.Extensions;
using Core.Shared.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Queries;
using Web.Endpoints.Routes;
using WorkflowTemplate.Shared.Queries.Template;
using WorkflowTemplate.Shared.Response.Template;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template;

public sealed class GetWorkflowTemplateDetailByIdEndpoint(ISender sender) : EndpointBaseAsync
    .WithRequest<GetWorkflowTemplateDetailByIdRequest>
    .WithActionResult<WorkflowTemplateDetailResponse>
{
    [ApiVersion("1.0")]
    [HttpGet(WorkflowTemplateRoutes.GetWorkflowTemplateById)]
    [ProducesResponseType(typeof(WorkflowTemplateDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
           Summary = "Obter detalhes de um template por Id",
           Description = "Retorna os dados completos de um template específico a partir do seu identificador único (GUID). Retorna 404 caso o template não seja encontrado.",
           Tags = [Tags.WorkflowTemplate])]
    public override async Task<ActionResult<WorkflowTemplateDetailResponse>> HandleAsync(
        GetWorkflowTemplateDetailByIdRequest request, 
        CancellationToken cancellationToken = default)
        => await Result.Create(request)
            .Map(request => new GetWorkflowTemplateDetailByIdQuery(request.Id))
            .Bind(query => sender.Send(query, cancellationToken))
            .Match(Ok, this.HandleFailure);
}