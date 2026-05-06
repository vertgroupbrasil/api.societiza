using Core.Application.Messaging.Queries;
using Core.Application.Pagination;
using Core.Application.Services.Interfaces;
using Core.Domain.Constants;
using Core.Domain.Pagination;
using Core.Domain.Pagination.Interfaces;
using Core.Shared.Results;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;
using WorkflowTemplate.Shared.Queries.Template;
using WorkflowTemplate.Shared.Response.Template;

namespace WorkflowTemplate.Application.UseCases.Queries.Template;

public class GetAllWorkflowTemplatesHandler(
    IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate> projectionGateway,
    ICurrentUserService currentUserService) : IQueryHandler<GetAllWorkflowTemplatesQuery, IPagedResult<WorkflowTemplateResponse>>
{
    public async Task<Result<IPagedResult<WorkflowTemplateResponse>>> Handle(GetAllWorkflowTemplatesQuery request, CancellationToken cancellationToken)
    {
        var isSystemAdmin = currentUserService.Role == IdentityRoles.SystemAdmin;

        var templates = await projectionGateway.ListAsync(x =>
            !x.IsDeleted && (
                x.Status == TemplateStatus.Active.Name ||
                (isSystemAdmin && request.IncludeDraft   && x.Status == TemplateStatus.Draft.Name)    ||
                (isSystemAdmin && request.IncludeArchive && x.Status == TemplateStatus.Archived.Name)
            ),
            cancellationToken);

        var response = templates
            .OrderBy(t => t.Name)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new WorkflowTemplateResponse(
                t.Id, t.Name, t.Description, t.TemplateVersion, t.Status, t.CreatedAt))
            .AsQueryable();

        var paging = new Paging(request.PageSize, request.PageNumber);
        return Result.Success(PagedResult<WorkflowTemplateResponse>.Create(paging, response));
    }
}
