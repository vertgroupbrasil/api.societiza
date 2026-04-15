namespace WorkflowProcess.Shared.Snapshots;

public sealed record TemplateStepFieldSnapshot(
    Guid FieldId,
    string Label,
    string FieldType,
    string? Options,
    int Order
);
