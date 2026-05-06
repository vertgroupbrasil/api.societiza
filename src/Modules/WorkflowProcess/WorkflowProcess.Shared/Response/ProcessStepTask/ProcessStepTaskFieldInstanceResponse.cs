namespace WorkflowProcess.Shared.Response.ProcessStepTask;

public sealed record ProcessStepTaskFieldInstanceResponse(
    Guid Id,
    string Label,
    string FieldType,
    string? Options,
    int Order,
    string? Value,
    DateTimeOffset? FilledAt
);