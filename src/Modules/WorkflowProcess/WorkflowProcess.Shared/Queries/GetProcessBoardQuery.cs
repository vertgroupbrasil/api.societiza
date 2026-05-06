using Core.Application.Messaging.Queries;
using WorkflowProcess.Shared.Response.Process;

namespace WorkflowProcess.Shared.Queries;

public sealed record GetProcessBoardQuery(
    Guid AccountancyId,
    string? ProcessType
) : IQuery<List<ProcessBoardItemResponse>>;