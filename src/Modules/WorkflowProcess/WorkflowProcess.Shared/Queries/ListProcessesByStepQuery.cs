using Core.Application.Messaging.Queries;
using Core.Domain.Pagination.Interfaces;
using WorkflowProcess.Shared.Response.Process;

namespace WorkflowProcess.Shared.Queries;

public sealed record ListProcessesByStepQuery(
    Guid AccountancyId,
    int PageNumber,
    int PageSize
) : IQuery<IPagedResult<ProcessStepGroupResponse>>;
