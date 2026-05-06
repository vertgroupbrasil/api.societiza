using Core.Domain.Primitives;
using WorkflowProcess.Domain.Enumerations;

namespace WorkflowProcess.Domain.Entities;

public class ProcessStepTaskInstance : Entity
{
    public Guid StepInstanceId { get; private set; }
    public Guid TemplateTaskId { get; private set; }
    public string Title { get; private set; } = null!;
    public int Order { get; private set; }
    public bool IsOptional { get; private set; }
    public TaskInstanceStatus Status { get; private set; } = TaskInstanceStatus.Pending;
    public List<ProcessStepTaskFieldInstance> Fields { get; private set; } = [];
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? SkippedAt { get; private set; }

    public ProcessStepTaskInstance(Guid id, Guid stepInstanceId, Guid templateTaskId, string title, int order, bool isOptional)
    {
        Id = id;
        StepInstanceId = stepInstanceId;
        TemplateTaskId = templateTaskId;
        Title = title;
        Order = order;
        IsOptional = isOptional;
    }

    public static ProcessStepTaskInstance Create(Guid id, Guid stepInstanceId, Guid templateTaskId, string title, int order, bool isOptional)
        => new(id, stepInstanceId, templateTaskId, title, order, isOptional);

    internal void MarkCompleted(DateTimeOffset completedAt)
    {
        Status = TaskInstanceStatus.Completed;
        CompletedAt = completedAt;
        UpdatedAt = completedAt;
    }

    internal void MarkSkipped(DateTimeOffset skippedAt)
    {
        Status = TaskInstanceStatus.Skipped;
        SkippedAt = skippedAt;
        UpdatedAt = skippedAt;
    }

    internal void MarkReverted(DateTimeOffset revertedAt)
    {
        Status = TaskInstanceStatus.Pending;
        CompletedAt = null;
        SkippedAt = null;
        UpdatedAt = revertedAt;
    }

    public void AddField(ProcessStepTaskFieldInstance field)
        => Fields.Add(field);
}