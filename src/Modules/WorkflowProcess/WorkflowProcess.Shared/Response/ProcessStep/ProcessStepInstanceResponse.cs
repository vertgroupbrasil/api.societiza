using WorkflowProcess.Shared.Response.ProcessStepTask;

namespace WorkflowProcess.Shared.Response.ProcessStep;

public sealed record ProcessStepInstanceResponse(
    Guid Id,
    string Title,
    int Order,
    string Status,
    DateTimeOffset? CompletedAt,
    List<ProcessStepTaskInstanceResponse> Tasks,
    List<ProcessStepFieldInstanceResponse> Fields
);
