namespace WorkflowProcess.Shared.Snapshots;

public sealed record TemplateStepTaskSnapshot(
    Guid TaskId,
    string Title,
    string Description,
    string Type,
    string Configuration,
    int Order,
    bool IsOptional,
    List<TemplateStepTaskFieldSnapshot> Fields
);