namespace WorkflowProcess.Shared.Response.ProcessStep;

public sealed record ProcessStepFieldInstanceResponse(
    Guid Id,
    string Label,
    string FieldType,
    string? Options,
    int Order,
    string? Value,
    DateTimeOffset? FilledAt
);