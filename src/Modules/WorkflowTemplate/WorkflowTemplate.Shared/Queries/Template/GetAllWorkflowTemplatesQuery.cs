using Core.Application.Messaging.Queries;
using Core.Domain.Pagination.Interfaces;
using WorkflowTemplate.Shared.Response.Template;

namespace WorkflowTemplate.Shared.Queries.Template;

public sealed record GetAllWorkflowTemplatesQuery(
    bool IncludeDraft    = false,
    bool IncludeArchive  = false,
    int  PageNumber      = 1,
    int  PageSize        = 10
) : IQuery<IPagedResult<WorkflowTemplateResponse>>;
