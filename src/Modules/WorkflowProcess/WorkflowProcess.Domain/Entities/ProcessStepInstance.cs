using Core.Domain.Primitives;
using WorkflowProcess.Domain.Enumerations;

namespace WorkflowProcess.Domain.Entities;

public class ProcessStepInstance : Entity
{
    public Guid ProcessId { get; private set; }
    public Guid TemplateStepId { get; private set; }
    public string Title { get; private set; } = null!;
    public int Order { get; private set; }
    public StepInstanceStatus Status { get; private set; } = StepInstanceStatus.NotStarted;
    public List<ProcessStepTaskInstance> Tasks { get; private set; } = [];
    public List<ProcessStepFieldInstance> Fields { get; private set; } = [];
    public DateTimeOffset? CompletedAt { get; private set; }

    public ProcessStepInstance(Guid id, Guid processId, Guid templateStepId, string title, int order)
    {
        Id = id;
        ProcessId = processId;
        TemplateStepId = templateStepId;
        Title = title;
        Order = order;
    }

    public static ProcessStepInstance Create(Guid id, Guid processId, Guid templateStepId, string title, int order)
        => new(id, processId, templateStepId, title, order);

    internal void Start(DateTimeOffset updatedAt)
    {
        Status = StepInstanceStatus.InProgress;
        UpdatedAt = updatedAt;
    }

    internal void Complete(DateTimeOffset completedAt)
    {
        Status = StepInstanceStatus.Completed;
        CompletedAt = completedAt;
        UpdatedAt = completedAt;
    }

    internal void Revert(StepInstanceStatus newStatus, DateTimeOffset revertedAt)
    {
        Status = newStatus;
        CompletedAt = null;
        UpdatedAt = revertedAt;
    }

    public void AddTask(ProcessStepTaskInstance task)
        => Tasks.Add(task);

    public void AddField(ProcessStepFieldInstance field)
        => Fields.Add(field);

    public bool AreAllRequiredTasksResolved()
        => Tasks
            .Where(t => !t.IsOptional)
            .All(t => t.Status == TaskInstanceStatus.Completed || t.Status == TaskInstanceStatus.Skipped);
}