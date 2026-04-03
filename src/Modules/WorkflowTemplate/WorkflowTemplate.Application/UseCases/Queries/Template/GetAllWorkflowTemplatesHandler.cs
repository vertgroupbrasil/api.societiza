using Core.Application.Messaging.Queries;
using Core.Shared.Results;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;
using WorkflowTemplate.Shared.Queries.Template;
using WorkflowTemplate.Shared.Response.Template;

namespace WorkflowTemplate.Application.UseCases.Queries.Template;

public class GetAllWorkflowTemplatesHandler(
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate> projectionGateway) : IQueryHandler<GetAllWorkflowTemplatesQuery, List<WorkflowTemplateResponse>>
{
    public async Task<Result<List<WorkflowTemplateResponse>>> Handle(GetAllWorkflowTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await projectionGateway.ListAsync(x => !x.IsDeleted, cancellationToken);

        var response = templates
            .OrderBy(t => t.Name)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new WorkflowTemplateResponse(
                t.Id,
                t.Name,
                t.Description,
                t.TemplateVersion,
                t.Status,
                t.CreatedAt,
                t.SourceTemplateId,
                t.SourceTemplateId.HasValue))
            .ToList();

        return Result.Success(response);
    }
}
