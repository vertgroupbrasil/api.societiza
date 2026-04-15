using Core.Application.Messaging.Queries;
using WorkflowProcess.Shared.Response.Process;

namespace WorkflowProcess.Shared.Queries;

public sealed record GetProcessDetailQuery(
    Guid ProcessId
) : IQuery<ProcessDetailResponse>;
