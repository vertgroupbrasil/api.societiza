using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using WorkflowTemplate.Domain.Entities;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.ValueObjects;

namespace WorkflowTemplate.Domain.Aggregates;

public class WorkflowTemplate : AggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public TemplateVersion TemplateVersion { get; private set; } = new TemplateVersion(1, 0);
    public TemplateStatus Status { get; private set; }
    public List<WorkflowTemplateStep> Steps { get; private set; } = [];

    #region Workflow Template Methods
    public static WorkflowTemplate Create(string name, string description)
    {
        WorkflowTemplate workflowTemplate = new();

        workflowTemplate.RaiseEvent<DomainEvents.WorkflowTemplateCreated>(version => new(
            Id: workflowTemplate.Id,
            Name: name,
            Description: description,
            TemplateVersion: workflowTemplate.TemplateVersion.ToString(),
            Status: TemplateStatus.Draft.ToString(),
            CreatedAt: workflowTemplate.CreatedAt,
            Version: version
        ));

        return workflowTemplate;
    }

    public void Update(string name, string description)
    {
        RaiseEvent<DomainEvents.WorkflowTemplateUpdated>(version => new(
            Id: Id,
            Name: name,
            Description: description,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void Activate()
        => RaiseEvent<DomainEvents.WorkflowTemplateActivated>(version => new(Id: Id, DateTimeOffset.UtcNow, Version: version));

    public void Archive()
        => RaiseEvent<DomainEvents.WorkflowTemplateArchived>(version => new(Id: Id, DateTimeOffset.UtcNow, Version: version));
    #endregion

    #region Workflow Template Step Methods
    public Guid AddStep(string title, string description, int order)
    {
        var stepId = Guid.NewGuid();

        RaiseEvent<DomainEvents.WorkflowTemplateStepAdded>(version => new(
            TemplateId: Id,
            StepId: stepId,
            Title: title,
            Description: description,
            Order: order,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return stepId;
    }

    public void UpdateStep(Guid stepId, string title, string description, int order)
    {
        RaiseEvent<DomainEvents.WorkflowTemplateStepUpdated>(version => new(
            TemplateId: Id,
            StepId: stepId,
            Title: title,
            Description: description,
            Order: order,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RemoveStep(Guid stepId)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId);

        if (step is null) return;

        foreach (var field in step.Fields.ToList())
            RaiseEvent<DomainEvents.WorkflowTemplateStepFieldRemoved>(version => new(TemplateId: Id, StepId: stepId, FieldId: field.Id, Version: version));

        foreach (var task in step.Tasks.ToList())
        {
            foreach (var taskField in task.Fields.ToList())
                RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldRemoved>(version => new(TemplateId: Id, StepId: stepId, TaskId: task.Id, FieldId: taskField.Id, Version: version));

            RaiseEvent<DomainEvents.WorkflowTemplateStepTaskRemoved>(version => new(TemplateId: Id, StepId: stepId, TaskId: task.Id, Version: version));
        }

        RaiseEvent<DomainEvents.WorkflowTemplateStepRemoved>(version => new(TemplateId: Id, StepId: stepId, Version: version));
    }
    #endregion

    #region Workflow Template Step Tasks Methods
    public Guid AddTask(Guid stepId, string title, string description, TaskType type, string configuration, int order, bool isOptional)
    {
        var taskId = Guid.NewGuid();

        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskAdded>(version => new(
            TemplateId: Id,
            StepId: stepId,
            TaskId: taskId,
            Title: title,
            Description: description,
            Type: type.ToString(),
            Configuration: configuration,
            Order: order,
            IsOptional: isOptional,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return taskId;
    }

    public void UpdateTask(Guid stepId, Guid taskId, string title, string description, TaskType type, string configuration, int order, bool isOptional)
    {
        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskUpdated>(version => new(
            TemplateId: Id,
            StepId: stepId,
            TaskId: taskId,
            Title: title,
            Description: description,
            Type: type.ToString(),
            Configuration: configuration,
            Order: order,
            IsOptional: isOptional,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RemoveTask(Guid stepId, Guid taskId)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId);
        var task = step?.Tasks.FirstOrDefault(t => t.Id == taskId);

        if (task is not null)
        {
            foreach (var field in task.Fields.ToList())
                RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldRemoved>(version => new(TemplateId: Id, StepId: stepId, TaskId: taskId, FieldId: field.Id, Version: version));
        }

        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskRemoved>(version => new(TemplateId: Id, StepId: stepId, TaskId: taskId, Version: version));
    }
    #endregion

    #region Workflow Template Step Field Methods
    public Guid AddStepField(Guid stepId, string label, FieldType fieldType, string? options)
    {
        var fieldId = Guid.NewGuid();

        RaiseEvent<DomainEvents.WorkflowTemplateStepFieldAdded>(version => new(
            TemplateId: Id,
            StepId: stepId,
            FieldId: fieldId,
            Label: label,
            FieldType: fieldType.ToString(),
            Options: options,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return fieldId;
    }

    public void UpdateStepField(Guid stepId, Guid fieldId, string label, FieldType fieldType, string? options)
    {
        RaiseEvent<DomainEvents.WorkflowTemplateStepFieldUpdated>(version => new(
            TemplateId: Id,
            StepId: stepId,
            FieldId: fieldId,
            Label: label,
            FieldType: fieldType.ToString(),
            Options: options,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RemoveStepField(Guid stepId, Guid fieldId)
        => RaiseEvent<DomainEvents.WorkflowTemplateStepFieldRemoved>(version => new(TemplateId: Id, StepId: stepId, FieldId: fieldId, Version: version));
    #endregion

    #region Workflow Template Step Task Field Methods
    public Guid AddTaskField(Guid stepId, Guid taskId, string label, FieldType fieldType, string? options)
    {
        var fieldId = Guid.NewGuid();

        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldAdded>(version => new(
            TemplateId: Id,
            StepId: stepId,
            TaskId: taskId,
            FieldId: fieldId,
            Label: label,
            FieldType: fieldType.ToString(),
            Options: options,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return fieldId;
    }

    public void UpdateTaskField(Guid stepId, Guid taskId, Guid fieldId, string label, FieldType fieldType, string? options)
    {
        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldUpdated>(version => new(
            TemplateId: Id,
            StepId: stepId,
            TaskId: taskId,
            FieldId: fieldId,
            Label: label,
            FieldType: fieldType.ToString(),
            Options: options,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RemoveTaskField(Guid stepId, Guid taskId, Guid fieldId)
        => RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldRemoved>(version => new(TemplateId: Id, StepId: stepId, TaskId: taskId, FieldId: fieldId, Version: version));
    #endregion

    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    #region Workflow Template Event Handlers
    private void When(DomainEvents.WorkflowTemplateCreated @event)
    {
        Id = @event.Id;
        Name = @event.Name;
        Description = @event.Description;
        TemplateVersion = TemplateVersion.Parse(@event.TemplateVersion);
        Status = Enum.Parse<TemplateStatus>(@event.Status);
    }

    private void When(DomainEvents.WorkflowTemplateUpdated @event)
    {
        Name = @event.Name;
        Description = @event.Description;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.WorkflowTemplateActivated @event)
    {
        Status = TemplateStatus.Active;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.WorkflowTemplateArchived @event)
    {
        Status = TemplateStatus.Archived;
        UpdatedAt = @event.UpdatedAt;
    }
    #endregion

    #region Workflow Template Step Event Handlers
    private void When(DomainEvents.WorkflowTemplateStepAdded @event)
        => Steps.Add(WorkflowTemplateStep.Create(@event.StepId, @event.TemplateId, @event.Title, @event.Description, @event.Order));

    private void When(DomainEvents.WorkflowTemplateStepUpdated @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        if (step is not null)
            step.Update(@event.Title, @event.Description, @event.Order, @event.UpdatedAt);
    }

    private void When(DomainEvents.WorkflowTemplateStepRemoved @event)
        => Steps.RemoveAll(s => s.Id == @event.StepId);
    #endregion

    #region Workflow Template Step Tasks Event Handlers
    private void When(DomainEvents.WorkflowTemplateStepTaskAdded @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        step?.AddTask(WorkflowTemplateStepTask.Create(@event.TaskId, @event.Title, @event.Description, Enum.Parse<TaskType>(@event.Type), @event.Configuration, @event.Order, @event.IsOptional));
    }

    private void When(DomainEvents.WorkflowTemplateStepTaskUpdated @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        step?.UpdateTask(@event.TaskId, @event.Title, @event.Description, Enum.Parse<TaskType>(@event.Type), @event.Configuration, @event.Order, @event.IsOptional, @event.UpdatedAt);
    }

    private void When(DomainEvents.WorkflowTemplateStepTaskRemoved @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        step?.RemoveTask(@event.TaskId);
    }
    #endregion

    #region Workflow Template Step Field Event Handlers
    private void When(DomainEvents.WorkflowTemplateStepFieldAdded @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        step?.AddField(TemplateStepField.Create(@event.FieldId, @event.StepId, @event.Label, Enum.Parse<FieldType>(@event.FieldType), @event.Options));
    }

    private void When(DomainEvents.WorkflowTemplateStepFieldUpdated @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        step?.UpdateField(@event.FieldId, @event.Label, Enum.Parse<FieldType>(@event.FieldType), @event.Options, @event.UpdatedAt);
    }

    private void When(DomainEvents.WorkflowTemplateStepFieldRemoved @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        step?.RemoveField(@event.FieldId);
    }
    #endregion

    #region Workflow Template Step Task Field Event Handlers
    private void When(DomainEvents.WorkflowTemplateStepTaskFieldAdded @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        var task = step?.Tasks.FirstOrDefault(t => t.Id == @event.TaskId);

        task?.AddField(TemplateStepTaskField.Create(@event.FieldId, @event.TaskId, @event.Label, Enum.Parse<FieldType>(@event.FieldType), @event.Options));
    }

    private void When(DomainEvents.WorkflowTemplateStepTaskFieldUpdated @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        var task = step?.Tasks.FirstOrDefault(t => t.Id == @event.TaskId);

        task?.UpdateField(@event.FieldId, @event.Label, Enum.Parse<FieldType>(@event.FieldType), @event.Options, @event.UpdatedAt);
    }

    private void When(DomainEvents.WorkflowTemplateStepTaskFieldRemoved @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        var task = step?.Tasks.FirstOrDefault(t => t.Id == @event.TaskId);

        task?.RemoveField(@event.FieldId);
    }
    #endregion
}