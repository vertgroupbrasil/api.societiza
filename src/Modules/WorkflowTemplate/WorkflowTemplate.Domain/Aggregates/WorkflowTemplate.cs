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
    public TemplateStatus Status { get; private set; } = TemplateStatus.Draft;
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
            Status: workflowTemplate.Status.Name,
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
    {
        RaiseEvent<DomainEvents.WorkflowTemplateActivated>(version => new(
            Id: Id,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version));
    }

    public void Archive()
    {
        RaiseEvent<DomainEvents.WorkflowTemplateArchived>(version => new(
            Id: Id,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version));
    }
    #endregion

    #region Workflow Template Step Methods
    public Guid AddStep(string title, string description, int order, List<string> appliesTo)
    {
        var resolvedOrder = ResolveStepOrder(order);
        var stepId = Guid.NewGuid();

        RaiseEvent<DomainEvents.WorkflowTemplateStepAdded>(version => new(
            TemplateId: Id,
            StepId: stepId,
            Title: title,
            Description: description,
            Order: resolvedOrder,
            AppliesTo: appliesTo,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return stepId;
    }

    public void UpdateStep(Guid stepId, string title, string description, int order, List<string> appliesTo)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null)
            return;

        var resolvedOrder = ResolveStepOrder(order, excludeStepId: stepId);

        RaiseEvent<DomainEvents.WorkflowTemplateStepUpdated>(version => new(
            TemplateId: Id,
            StepId: stepId,
            Title: title,
            Description: description,
            Order: resolvedOrder,
            AppliesTo: appliesTo,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RemoveStep(Guid stepId)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null) 
            return;

        var fieldIds = step.Fields.Select(f => f.Id).ToList();
        var taskIds = step.Tasks.Select(t => t.Id).ToList();
        var taskFieldIds = step.Tasks.SelectMany(t => t.Fields).Select(f => f.Id).ToList();

        RaiseEvent<DomainEvents.WorkflowTemplateStepRemoved>(version => new(
            TemplateId: Id,
            StepId: stepId,
            FieldIds: fieldIds,
            TaskIds: taskIds,
            TaskFieldIds: taskFieldIds,
            Version: version
        ));
    }

    private int ResolveStepOrder(int requestedOrder, Guid? excludeStepId = null)
    {
        var siblings = Steps
            .Where(s => excludeStepId is null || s.Id != excludeStepId)
            .OrderBy(s => s.Order)
            .ToList();

        if (siblings.All(s => s.Order != requestedOrder))
            return requestedOrder;

        foreach (var sibling in siblings.Where(s => s.Order >= requestedOrder).ToList())
        {
            sibling.ShiftOrder(sibling.Order + 1);

            RaiseEvent<DomainEvents.WorkflowTemplateStepUpdated>(version => new(
                TemplateId: Id,
                StepId: sibling.Id,
                Title: sibling.Title,
                Description: sibling.Description,
                Order: sibling.Order,
                AppliesTo: sibling.AppliesTo,
                UpdatedAt: DateTimeOffset.UtcNow,
                Version: version));
        }

        return requestedOrder;
    }
    #endregion

    #region Workflow Template Step Tasks Methods
    public Guid AddTask(Guid stepId, string title, string description, TaskType type, string configuration, int order, bool isOptional)
    {
        var resolvedOrder = ResolveTaskOrder(stepId, order);
        var taskId = Guid.NewGuid();

        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskAdded>(version => new(
            TemplateId: Id,
            StepId: stepId,
            TaskId: taskId,
            Title: title,
            Description: description,
            Type: type.Name,
            Configuration: configuration,
            Order: resolvedOrder,
            IsOptional: isOptional,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return taskId;
    }

    public void UpdateTask(Guid stepId, Guid taskId, string title, string description, TaskType type, string configuration, int order, bool isOptional)
    {
        var resolvedOrder = ResolveTaskOrder(stepId, order, excludeTaskId: taskId);

        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskUpdated>(version => new(
            TemplateId: Id,
            StepId: stepId,
            TaskId: taskId,
            Title: title,
            Description: description,
            Type: type.Name,
            Configuration: configuration,
            Order: resolvedOrder,
            IsOptional: isOptional,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RemoveTask(Guid stepId, Guid taskId)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId);
        var task = step?.Tasks.FirstOrDefault(t => t.Id == taskId);

        var taskFieldIds = task?.Fields.Select(f => f.Id).ToList() ?? [];

        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskRemoved>(version => new(
            TemplateId: Id,
            StepId: stepId,
            TaskId: taskId,
            TaskFieldIds: taskFieldIds,
            Version: version
        ));
    }

    private int ResolveTaskOrder(Guid stepId, int requestedOrder, Guid? excludeTaskId = null)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null) 
            return requestedOrder;

        var siblings = step.Tasks
            .Where(t => excludeTaskId is null || t.Id != excludeTaskId)
            .OrderBy(t => t.Order)
            .ToList();

        if (siblings.All(t => t.Order != requestedOrder))
            return requestedOrder;

        foreach (var sibling in siblings.Where(t => t.Order >= requestedOrder).ToList())
        {
            sibling.ShiftOrder(sibling.Order + 1);

            RaiseEvent<DomainEvents.WorkflowTemplateStepTaskUpdated>(version => new(
                TemplateId: Id,
                StepId: stepId,
                TaskId: sibling.Id,
                Title: sibling.Title,
                Description: sibling.Description,
                Type: sibling.Type.Name,
                Configuration: sibling.Configuration,
                Order: sibling.Order,
                IsOptional: sibling.IsOptional,
                UpdatedAt: DateTimeOffset.UtcNow,
                Version: version));
        }

        return requestedOrder;
    }
    #endregion

    #region Workflow Template Step Field Methods
    public Guid AddStepField(Guid stepId, string label, FieldType fieldType, string? options, int order)
    {
        var resolvedOrder = ResolveStepFieldOrder(stepId, order);
        var fieldId = Guid.NewGuid();

        RaiseEvent<DomainEvents.WorkflowTemplateStepFieldAdded>(version => new(
            TemplateId: Id,
            StepId: stepId,
            FieldId: fieldId,
            Label: label,
            FieldType: fieldType.Name,
            Options: options,
            Order: resolvedOrder,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return fieldId;
    }

    public void UpdateStepField(Guid stepId, Guid fieldId, string label, FieldType fieldType, string? options, int order)
    {
        var resolvedOrder = ResolveStepFieldOrder(stepId, order, excludeFieldId: fieldId);

        RaiseEvent<DomainEvents.WorkflowTemplateStepFieldUpdated>(version => new(
            TemplateId: Id,
            StepId: stepId,
            FieldId: fieldId,
            Label: label,
            FieldType: fieldType.Name,
            Options: options,
            Order: resolvedOrder,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RemoveStepField(Guid stepId, Guid fieldId)
        => RaiseEvent<DomainEvents.WorkflowTemplateStepFieldRemoved>(version => new(TemplateId: Id, StepId: stepId, FieldId: fieldId, Version: version));

    private int ResolveStepFieldOrder(Guid stepId, int requestedOrder, Guid? excludeFieldId = null)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId)!;
        var siblings = step.Fields
            .Where(f => excludeFieldId is null || f.Id != excludeFieldId)
            .OrderBy(f => f.Order)
            .ToList();

        if (siblings.All(f => f.Order != requestedOrder))
            return requestedOrder;

        foreach (var sibling in siblings.Where(f => f.Order >= requestedOrder))
            sibling.ShiftOrder(sibling.Order + 1);

        return requestedOrder;
    }
    #endregion

    #region Workflow Template Step Task Field Methods
    public Guid AddTaskField(Guid stepId, Guid taskId, string label, FieldType fieldType, string? options, int order)
    {
        var resolvedOrder = ResolveTaskFieldOrder(stepId, taskId, order);
        var fieldId = Guid.NewGuid();

        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldAdded>(version => new(
            TemplateId: Id,
            StepId: stepId,
            TaskId: taskId,
            FieldId: fieldId,
            Label: label,
            FieldType: fieldType.Name,
            Options: options,
            Order: resolvedOrder,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
        return fieldId;
    }

    public void UpdateTaskField(Guid stepId, Guid taskId, Guid fieldId, string label, FieldType fieldType, string? options, int order)
    {
        var resolvedOrder = ResolveTaskFieldOrder(stepId, taskId, order, excludeFieldId: fieldId);

        RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldUpdated>(version => new(
            TemplateId: Id,
            StepId: stepId,
            TaskId: taskId,
            FieldId: fieldId,
            Label: label,
            FieldType: fieldType.Name,
            Options: options,
            Order: resolvedOrder,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RemoveTaskField(Guid stepId, Guid taskId, Guid fieldId)
        => RaiseEvent<DomainEvents.WorkflowTemplateStepTaskFieldRemoved>(version => new(TemplateId: Id, StepId: stepId, TaskId: taskId, FieldId: fieldId, Version: version));

    private int ResolveTaskFieldOrder(Guid stepId, Guid taskId, int requestedOrder, Guid? excludeFieldId = null)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId)!;
        var task = step.Tasks.FirstOrDefault(t => t.Id == taskId)!;
        var siblings = task.Fields
            .Where(f => excludeFieldId is null || f.Id != excludeFieldId)
            .OrderBy(f => f.Order)
            .ToList();

        if (siblings.All(f => f.Order != requestedOrder))
            return requestedOrder;

        foreach (var sibling in siblings.Where(f => f.Order >= requestedOrder))
            sibling.ShiftOrder(sibling.Order + 1);

        return requestedOrder;
    }
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
        Status = TemplateStatus.FromName(@event.Status);
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
        => Steps.Add(WorkflowTemplateStep.Create(@event.StepId, @event.TemplateId, @event.Title, @event.Description, @event.Order, @event.AppliesTo));

    private void When(DomainEvents.WorkflowTemplateStepUpdated @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        if (step is not null)
            step.Update(@event.Title, @event.Description, @event.Order, @event.AppliesTo, @event.UpdatedAt);
    }

    private void When(DomainEvents.WorkflowTemplateStepRemoved @event)
        => Steps.RemoveAll(s => s.Id == @event.StepId);
    #endregion

    #region Workflow Template Step Tasks Event Handlers
    private void When(DomainEvents.WorkflowTemplateStepTaskAdded @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        step?.AddTask(WorkflowTemplateStepTask.Create(@event.TaskId, @event.Title, @event.Description, TaskType.FromName(@event.Type), @event.Configuration, @event.Order, @event.IsOptional));
    }

    private void When(DomainEvents.WorkflowTemplateStepTaskUpdated @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        step?.UpdateTask(@event.TaskId, @event.Title, @event.Description, TaskType.FromName(@event.Type), @event.Configuration, @event.Order, @event.IsOptional, @event.UpdatedAt);
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
        step?.AddField(TemplateStepField.Create(@event.FieldId, @event.StepId, @event.Label, FieldType.FromName(@event.FieldType), @event.Options, @event.Order));
    }

    private void When(DomainEvents.WorkflowTemplateStepFieldUpdated @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        step?.UpdateField(@event.FieldId, @event.Label, FieldType.FromName(@event.FieldType), @event.Options, @event.Order, @event.UpdatedAt);
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

        task?.AddField(TemplateStepTaskField.Create(@event.FieldId, @event.TaskId, @event.Label, FieldType.FromName(@event.FieldType), @event.Options, @event.Order));
    }

    private void When(DomainEvents.WorkflowTemplateStepTaskFieldUpdated @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        var task = step?.Tasks.FirstOrDefault(t => t.Id == @event.TaskId);

        task?.UpdateField(@event.FieldId, @event.Label, FieldType.FromName(@event.FieldType), @event.Options, @event.Order, @event.UpdatedAt);
    }

    private void When(DomainEvents.WorkflowTemplateStepTaskFieldRemoved @event)
    {
        var step = Steps.FirstOrDefault(s => s.Id == @event.StepId);
        var task = step?.Tasks.FirstOrDefault(t => t.Id == @event.TaskId);

        task?.RemoveField(@event.FieldId);
    }
    #endregion
}