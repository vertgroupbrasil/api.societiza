namespace WorkflowProcess.Shared.Response.ProcessStepTask;

public sealed record ProcessStepTaskInstanceResponse(
    Guid Id,
    string Title,
    int Order,
    bool IsOptional,
    string Status,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? SkippedAt,
    List<ProcessStepTaskFieldInstanceResponse> Fields
);