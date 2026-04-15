using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using WorkflowProcess.Domain.Entities;
using WorkflowProcess.Domain.Enumerations;
using WorkflowProcess.Domain.Events;

namespace WorkflowProcess.Domain.Aggregates;

public class WorkflowProcess : AggregateRoot
{
    public Guid AccountancyId { get; private set; }
    public Guid TemplateId { get; private set; }
    public string TemplateVersion { get; private set; } = null!;
    public ProcessType ProcessType { get; private set; } = ProcessType.Abertura;
    public string TargetClient { get; private set; } = null!;
    public ProcessStatus Status { get; private set; } = ProcessStatus.InProgress;
    public List<ProcessStepInstance> Steps { get; private set; } = [];
    public DateTimeOffset? CompletedAt { get; private set; }

    #region Factory Method
    public static WorkflowProcess Create(Guid accountancyId, Guid templateId, string templateVersion, string processType, string targetClient)
    {
        WorkflowProcess process = new();

        process.RaiseEvent<DomainEvents.WorkflowProcessCreated>(version => new(
            Id: process.Id,
            AccountancyId: accountancyId,
            TemplateId: templateId,
            TemplateVersion: templateVersion,
            ProcessType: processType,
            TargetClient: targetClient,
            Status: process.Status.Name,
            CreatedAt: process.CreatedAt,
            Version: version
        ));

        return process;
    }
    #endregion

    #region Materialize Methods
    public Guid MaterializeStep(Guid templateStepId, string title, int order)
    {
        var stepInstanceId = Guid.NewGuid();

        RaiseEvent<DomainEvents.ProcessStepInstanceMaterialized>(version => new(
            ProcessId: Id,
            StepInstanceId: stepInstanceId,
            TemplateStepId: templateStepId,
            Title: title,
            Order: order,
            Status: StepInstanceStatus.NotStarted.Name,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return stepInstanceId;
    }

    public Guid MaterializeTask(Guid stepInstanceId, Guid templateTaskId, string title, int order, bool isOptional)
    {
        var taskInstanceId = Guid.NewGuid();

        RaiseEvent<DomainEvents.ProcessStepTaskInstanceMaterialized>(version => new(
            ProcessId: Id,
            StepInstanceId: stepInstanceId,
            TaskInstanceId: taskInstanceId,
            TemplateTaskId: templateTaskId,
            Title: title,
            Order: order,
            IsOptional: isOptional,
            Status: TaskInstanceStatus.Pending.Name,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return taskInstanceId;
    }

    public Guid MaterializeStepField(Guid stepInstanceId, Guid templateFieldId, string label, string fieldType, string? options, bool isRequired, int order)
    {
        var fieldInstanceId = Guid.NewGuid();

        RaiseEvent<DomainEvents.ProcessStepFieldInstanceMaterialized>(version => new(
            ProcessId: Id,
            StepInstanceId: stepInstanceId,
            FieldInstanceId: fieldInstanceId,
            TemplateFieldId: templateFieldId,
            Label: label,
            FieldType: fieldType,
            Options: options,
            IsRequired: isRequired,
            Order: order,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return fieldInstanceId;
    }

    public Guid MaterializeTaskField(Guid stepInstanceId, Guid taskInstanceId, Guid templateFieldId, string label, string fieldType, string? options, bool isRequired, int order)
    {
        var fieldInstanceId = Guid.NewGuid();

        RaiseEvent<DomainEvents.ProcessStepTaskFieldInstanceMaterialized>(version => new(
            ProcessId: Id,
            StepInstanceId: stepInstanceId,
            TaskInstanceId: taskInstanceId,
            FieldInstanceId: fieldInstanceId,
            TemplateFieldId: templateFieldId,
            Label: label,
            FieldType: fieldType,
            Options: options,
            IsRequired: isRequired,
            Order: order,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return fieldInstanceId;
    }
    #endregion

    #region Behavior Methods
    public void CompleteTask(Guid stepInstanceId, Guid taskInstanceId)
    {
        var now = DateTimeOffset.UtcNow;
        var step = Steps.First(s => s.Id == stepInstanceId);

        if (step.Status == StepInstanceStatus.NotStarted)
        {
            RaiseEvent<DomainEvents.ProcessStepInstanceStarted>(version => new(
                ProcessId: Id,
                StepInstanceId: stepInstanceId,
                UpdatedAt: now,
                Version: version
            ));
        }

        RaiseEvent<DomainEvents.ProcessStepTaskInstanceCompleted>(version => new(
            ProcessId: Id,
            StepInstanceId: stepInstanceId,
            TaskInstanceId: taskInstanceId,
            CompletedAt: now,
            Version: version
        ));

        if (step.AreAllRequiredTasksResolved())
        {
            RaiseEvent<DomainEvents.ProcessStepInstanceCompleted>(version => new(
                ProcessId: Id,
                StepInstanceId: stepInstanceId,
                CompletedAt: now,
                Version: version
            ));

            if (Steps.All(s => s.Status == StepInstanceStatus.Completed))
            {
                RaiseEvent<DomainEvents.WorkflowProcessCompleted>(version => new(
                    ProcessId: Id,
                    CompletedAt: now,
                    Version: version
                ));
            }
        }
    }

    public void SkipTask(Guid stepInstanceId, Guid taskInstanceId)
    {
        var now = DateTimeOffset.UtcNow;
        var step = Steps.First(s => s.Id == stepInstanceId);

        if (step.Status == StepInstanceStatus.NotStarted)
        {
            RaiseEvent<DomainEvents.ProcessStepInstanceStarted>(version => new(
                ProcessId: Id,
                StepInstanceId: stepInstanceId,
                UpdatedAt: now,
                Version: version
            ));
        }

        RaiseEvent<DomainEvents.ProcessStepTaskInstanceSkipped>(version => new(
            ProcessId: Id,
            StepInstanceId: stepInstanceId,
            TaskInstanceId: taskInstanceId,
            SkippedAt: now,
            Version: version
        ));

        if (step.AreAllRequiredTasksResolved())
        {
            RaiseEvent<DomainEvents.ProcessStepInstanceCompleted>(version => new(
                ProcessId: Id,
                StepInstanceId: stepInstanceId,
                CompletedAt: now,
                Version: version
            ));

            if (Steps.All(s => s.Status == StepInstanceStatus.Completed))
            {
                RaiseEvent<DomainEvents.WorkflowProcessCompleted>(version => new(
                    ProcessId: Id,
                    CompletedAt: now,
                    Version: version
                ));
            }
        }
    }

    public void FillStepField(Guid stepInstanceId, Guid fieldInstanceId, string value)
    {
        RaiseEvent<DomainEvents.ProcessStepFieldInstanceFilled>(version => new(
            ProcessId: Id,
            StepInstanceId: stepInstanceId,
            FieldInstanceId: fieldInstanceId,
            Value: value,
            FilledAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void FillTaskField(Guid stepInstanceId, Guid taskInstanceId, Guid fieldInstanceId, string value)
    {
        RaiseEvent<DomainEvents.ProcessStepTaskFieldInstanceFilled>(version => new(
            ProcessId: Id,
            StepInstanceId: stepInstanceId,
            TaskInstanceId: taskInstanceId,
            FieldInstanceId: fieldInstanceId,
            Value: value,
            FilledAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }
    #endregion

    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    #region Workflow Process Event Handlers
    private void When(DomainEvents.WorkflowProcessCreated @event)
    {
        Id = @event.Id;
        AccountancyId = @event.AccountancyId;
        TemplateId = @event.TemplateId;
        TemplateVersion = @event.TemplateVersion;
        ProcessType = ProcessType.FromName(@event.ProcessType);
        TargetClient = @event.TargetClient;
        Status = ProcessStatus.FromName(@event.Status);
    }

    private void When(DomainEvents.WorkflowProcessCompleted @event)
    {
        Status = ProcessStatus.Completed;
        CompletedAt = @event.CompletedAt;
    }
    #endregion

    #region Process Step Instance Event Handlers
    private void When(DomainEvents.ProcessStepInstanceMaterialized @event)
        => Steps.Add(ProcessStepInstance.Create(
            @event.StepInstanceId,
            @event.ProcessId,
            @event.TemplateStepId,
            @event.Title,
            @event.Order));

    private void When(DomainEvents.ProcessStepInstanceStarted @event)
    {
        var step = Steps.First(s => s.Id == @event.StepInstanceId);
        step.Start(@event.UpdatedAt);
    }

    private void When(DomainEvents.ProcessStepInstanceCompleted @event)
    {
        var step = Steps.First(s => s.Id == @event.StepInstanceId);
        step.Complete(@event.CompletedAt);
    }
    #endregion

    #region Process Step Task Instance Event Handlers
    private void When(DomainEvents.ProcessStepTaskInstanceMaterialized @event)
    {
        var step = Steps.First(s => s.Id == @event.StepInstanceId);
        step.AddTask(ProcessStepTaskInstance.Create(
            @event.TaskInstanceId,
            @event.StepInstanceId,
            @event.TemplateTaskId,
            @event.Title,
            @event.Order,
            @event.IsOptional));
    }

    private void When(DomainEvents.ProcessStepTaskInstanceCompleted @event)
    {
        var step = Steps.First(s => s.Id == @event.StepInstanceId);
        var task = step.Tasks.First(t => t.Id == @event.TaskInstanceId);
        task.MarkCompleted(@event.CompletedAt);
    }

    private void When(DomainEvents.ProcessStepTaskInstanceSkipped @event)
    {
        var step = Steps.First(s => s.Id == @event.StepInstanceId);
        var task = step.Tasks.First(t => t.Id == @event.TaskInstanceId);
        task.MarkSkipped(@event.SkippedAt);
    }
    #endregion

    #region Process Step Field Instance Event Handlers
    private void When(DomainEvents.ProcessStepFieldInstanceMaterialized @event)
    {
        var step = Steps.First(s => s.Id == @event.StepInstanceId);
        step.AddField(ProcessStepFieldInstance.Create(
            @event.FieldInstanceId,
            @event.StepInstanceId,
            @event.TemplateFieldId,
            @event.Label,
            @event.FieldType,
            @event.Options,
            @event.IsRequired,
            @event.Order));
    }

    private void When(DomainEvents.ProcessStepFieldInstanceFilled @event)
    {
        var step = Steps.First(s => s.Id == @event.StepInstanceId);
        var field = step.Fields.First(f => f.Id == @event.FieldInstanceId);
        field.Fill(@event.Value, @event.FilledAt);
    }
    #endregion

    #region Process Step Task Field Instance Event Handlers
    private void When(DomainEvents.ProcessStepTaskFieldInstanceMaterialized @event)
    {
        var step = Steps.First(s => s.Id == @event.StepInstanceId);
        var task = step.Tasks.First(t => t.Id == @event.TaskInstanceId);
        task.AddField(ProcessStepTaskFieldInstance.Create(
            @event.FieldInstanceId,
            @event.TaskInstanceId,
            @event.TemplateFieldId,
            @event.Label,
            @event.FieldType,
            @event.Options,
            @event.IsRequired,
            @event.Order));
    }

    private void When(DomainEvents.ProcessStepTaskFieldInstanceFilled @event)
    {
        var step = Steps.First(s => s.Id == @event.StepInstanceId);
        var task = step.Tasks.First(t => t.Id == @event.TaskInstanceId);
        var field = task.Fields.First(f => f.Id == @event.FieldInstanceId);
        field.Fill(@event.Value, @event.FilledAt);
    }
    #endregion
}