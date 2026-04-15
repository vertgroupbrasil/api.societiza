namespace WorkflowProcess.Shared.Snapshots;

public sealed record TemplateStepTaskFieldSnapshot(
    Guid FieldId,
    string Label,
    string FieldType,
    string? Options,
    int Order
);
