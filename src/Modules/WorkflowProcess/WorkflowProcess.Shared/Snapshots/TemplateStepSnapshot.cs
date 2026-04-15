namespace WorkflowProcess.Shared.Snapshots;

public sealed record TemplateStepSnapshot(
    Guid StepId,
    string Title,
    string Description,
    int Order,
    List<TemplateStepTaskSnapshot> Tasks,
    List<TemplateStepFieldSnapshot> Fields
);
