using Core.Domain.Events.Interfaces;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Exceptions;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Domain.Aggregates;

public class WorkflowTemplateAggregateTests
{
    #region Create

    [Fact]
    public void Create_SetsNameAndDescription()
    {
        var template = CreateTemplate();

        Assert.Equal("My Template", template.Name);
        Assert.Equal("My Description", template.Description);
    }

    [Fact]
    public void Create_SetsStatusToDraft()
    {
        var template = CreateTemplate();

        Assert.Equal(TemplateStatus.Draft, template.Status);
    }

    [Fact]
    public void Create_SetsInitialVersion()
    {
        var template = CreateTemplate();

        Assert.Equal("v1.0", template.TemplateVersion.ToString());
    }

    [Fact]
    public void Create_SetsVersionToOne()
    {
        var template = CreateTemplate();
        template.TryDequeueEvent(out _);

        Assert.Equal(1UL, template.Version);
    }

    [Fact]
    public void Create_RaisesWorkflowTemplateCreatedEvent()
    {
        var template = CreateTemplate();

        Assert.True(template.TryDequeueEvent(out IDomainEvent @event));
        Assert.IsType<DomainEvents.WorkflowTemplateCreated>(@event);
    }

    [Fact]
    public void Create_CreatedEvent_ContainsCorrectData()
    {
        var template = CreateTemplate();
        template.TryDequeueEvent(out var domainEvent);
        var @event = Assert.IsType<DomainEvents.WorkflowTemplateCreated>(domainEvent);

        Assert.Equal(template.Id, @event.Id);
        Assert.Equal("My Template", @event.Name);
        Assert.Equal("My Description", @event.Description);
        Assert.Equal(TemplateStatus.Draft.Name, @event.Status);
        Assert.Equal("v1.0", @event.TemplateVersion);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_ChangesNameAndDescription()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.Update("New Name", "New Description");

        Assert.Equal("New Name", template.Name);
        Assert.Equal("New Description", template.Description);
    }

    [Fact]
    public void Update_SetsUpdatedAt()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.Update("New Name", "New Description");

        Assert.NotNull(template.UpdatedAt);
    }

    [Fact]
    public void Update_RaisesWorkflowTemplateUpdatedEvent()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.Update("New Name", "New Description");

        Assert.True(template.TryDequeueEvent(out var @event));
        Assert.IsType<DomainEvents.WorkflowTemplateUpdated>(@event);
    }

    [Fact]
    public void Update_IncrementsVersion()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.Update("New Name", "New Description");

        Assert.Equal(2UL, template.Version);
    }

    #endregion

    #region Activate

    [Fact]
    public void Activate_SetsStatusToActive()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.Activate();

        Assert.Equal(TemplateStatus.Active, template.Status);
    }

    [Fact]
    public void Activate_RaisesWorkflowTemplateActivatedEvent()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.Activate();

        Assert.True(template.TryDequeueEvent(out var @event));
        Assert.IsType<DomainEvents.WorkflowTemplateActivated>(@event);
    }

    #endregion

    #region Archive

    [Fact]
    public void Archive_SetsStatusToArchived()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.Archive();

        Assert.Equal(TemplateStatus.Archived, template.Status);
    }

    [Fact]
    public void Archive_RaisesWorkflowTemplateArchivedEvent()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.Archive();

        Assert.True(template.TryDequeueEvent(out var @event));
        Assert.IsType<DomainEvents.WorkflowTemplateArchived>(@event);
    }

    #endregion

    #region AddStep

    [Fact]
    public void AddStep_ReturnsStepId()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        var stepId = template.AddStep("Step 1", "Description", 1);

        Assert.NotEqual(Guid.Empty, stepId);
    }

    [Fact]
    public void AddStep_AddsStepToCollection()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.AddStep("Step 1", "Description", 1);

        Assert.Single(template.Steps);
        Assert.Equal("Step 1", template.Steps[0].Title);
    }

    [Fact]
    public void AddStep_RaisesWorkflowTemplateStepAddedEvent()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.AddStep("Step 1", "Description", 1);

        Assert.True(template.TryDequeueEvent(out var @event));
        Assert.IsType<DomainEvents.WorkflowTemplateStepAdded>(@event);
    }

    [Fact]
    public void AddStep_WithDuplicateTitle_ThrowsDuplicateEntityException()
    {
        var template = CreateTemplate();
        DrainEvents(template);
        template.AddStep("Step 1", "Description", 1);
        DrainEvents(template);

        Assert.Throws<DuplicateEntityException>(() => template.AddStep("Step 1", "Another", 2));
    }

    #endregion

    #region UpdateStep

    [Fact]
    public void UpdateStep_ChangesStepProperties()
    {
        var template = CreateTemplate();
        DrainEvents(template);
        var stepId = template.AddStep("Step 1", "Old Description", 1);
        DrainEvents(template);

        template.UpdateStep(stepId, "Updated Step", "New Description", 2);

        var step = template.Steps.First(s => s.Id == stepId);
        Assert.Equal("Updated Step", step.Title);
        Assert.Equal("New Description", step.Description);
        Assert.Equal(2, step.Order);
    }

    [Fact]
    public void UpdateStep_RaisesWorkflowTemplateStepUpdatedEvent()
    {
        var template = CreateTemplate();
        DrainEvents(template);
        var stepId = template.AddStep("Step 1", "Description", 1);
        DrainEvents(template);

        template.UpdateStep(stepId, "Updated Step", "New Description", 2);

        Assert.True(template.TryDequeueEvent(out var @event));
        Assert.IsType<DomainEvents.WorkflowTemplateStepUpdated>(@event);
    }

    #endregion

    #region RemoveStep

    [Fact]
    public void RemoveStep_RemovesStepFromCollection()
    {
        var template = CreateTemplate();
        DrainEvents(template);
        var stepId = template.AddStep("Step 1", "Description", 1);
        DrainEvents(template);

        template.RemoveStep(stepId);

        Assert.Empty(template.Steps);
    }

    [Fact]
    public void RemoveStep_RaisesWorkflowTemplateStepRemovedEvent()
    {
        var template = CreateTemplate();
        DrainEvents(template);
        var stepId = template.AddStep("Step 1", "Description", 1);
        DrainEvents(template);

        template.RemoveStep(stepId);

        Assert.True(template.TryDequeueEvent(out var @event));
        Assert.IsType<DomainEvents.WorkflowTemplateStepRemoved>(@event);
    }

    [Fact]
    public void RemoveStep_WhenStepNotFound_DoesNotRaiseEvent()
    {
        var template = CreateTemplate();
        DrainEvents(template);
        template.AddStep("Step 1", "Description", 1);
        DrainEvents(template);

        template.RemoveStep(Guid.NewGuid());

        Assert.False(template.TryDequeueEvent(out _));
    }

    #endregion

    #region AddTask

    [Fact]
    public void AddTask_ReturnsTaskId()
    {
        var (template, stepId) = CreateTemplateWithStep();

        var taskId = template.AddTask(stepId, "Task 1", "Desc", TaskType.Manual, "{}", 1, false);

        Assert.NotEqual(Guid.Empty, taskId);
    }

    [Fact]
    public void AddTask_AddsTaskToStep()
    {
        var (template, stepId) = CreateTemplateWithStep();

        template.AddTask(stepId, "Task 1", "Desc", TaskType.Manual, "{}", 1, false);

        Assert.Single(template.Steps.First(s => s.Id == stepId).Tasks);
    }

    [Fact]
    public void AddTask_WhenStepNotFound_ThrowsArgumentException()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        Assert.Throws<ArgumentException>(() =>
            template.AddTask(Guid.NewGuid(), "Task 1", "Desc", TaskType.Manual, "{}", 1, false));
    }

    [Fact]
    public void AddTask_WithDuplicateTitle_ThrowsDuplicateEntityException()
    {
        var (template, stepId) = CreateTemplateWithStep();
        template.AddTask(stepId, "Task 1", "Desc", TaskType.Manual, "{}", 1, false);
        DrainEvents(template);

        Assert.Throws<DuplicateEntityException>(() =>
            template.AddTask(stepId, "Task 1", "Desc 2", TaskType.Automatic, "{}", 2, false));
    }

    #endregion

    #region AddStepField

    [Fact]
    public void AddStepField_ReturnsFieldId()
    {
        var (template, stepId) = CreateTemplateWithStep();

        var fieldId = template.AddStepField(stepId, "Field 1", FieldType.Text, null);

        Assert.NotEqual(Guid.Empty, fieldId);
    }

    [Fact]
    public void AddStepField_AddsFieldToStep()
    {
        var (template, stepId) = CreateTemplateWithStep();

        template.AddStepField(stepId, "Field 1", FieldType.Text, null);

        Assert.Single(template.Steps.First(s => s.Id == stepId).Fields);
    }

    [Fact]
    public void AddStepField_WhenStepNotFound_ThrowsArgumentException()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        Assert.Throws<ArgumentException>(() =>
            template.AddStepField(Guid.NewGuid(), "Field 1", FieldType.Text, null));
    }

    [Fact]
    public void AddStepField_WithDuplicateLabel_ThrowsDuplicateEntityException()
    {
        var (template, stepId) = CreateTemplateWithStep();
        template.AddStepField(stepId, "Field 1", FieldType.Text, null);
        DrainEvents(template);

        Assert.Throws<DuplicateEntityException>(() =>
            template.AddStepField(stepId, "Field 1", FieldType.Boolean, null));
    }

    #endregion

    #region AddTaskField

    [Fact]
    public void AddTaskField_ReturnsFieldId()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();

        var fieldId = template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null);

        Assert.NotEqual(Guid.Empty, fieldId);
    }

    [Fact]
    public void AddTaskField_WhenStepNotFound_ThrowsArgumentException()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        Assert.Throws<ArgumentException>(() =>
            template.AddTaskField(Guid.NewGuid(), Guid.NewGuid(), "Field 1", FieldType.Text, null));
    }

    [Fact]
    public void AddTaskField_WhenTaskNotFound_ThrowsArgumentException()
    {
        var (template, stepId) = CreateTemplateWithStep();

        Assert.Throws<ArgumentException>(() =>
            template.AddTaskField(stepId, Guid.NewGuid(), "Field 1", FieldType.Text, null));
    }

    [Fact]
    public void AddTaskField_WithDuplicateLabel_ThrowsDuplicateEntityException()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();
        template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null);
        DrainEvents(template);

        Assert.Throws<DuplicateEntityException>(() =>
            template.AddTaskField(stepId, taskId, "Field 1", FieldType.Boolean, null));
    }

    #endregion

    #region Helpers

    private static WorkflowTemplateAggregate CreateTemplate()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        return template;
    }

    private static (WorkflowTemplateAggregate template, Guid stepId) CreateTemplateWithStep()
    {
        var template = CreateTemplate();
        DrainEvents(template);
        var stepId = template.AddStep("Step 1", "Step Description", 1);
        DrainEvents(template);
        return (template, stepId);
    }

    private static (WorkflowTemplateAggregate template, Guid stepId, Guid taskId) CreateTemplateWithStepAndTask()
    {
        var (template, stepId) = CreateTemplateWithStep();
        var taskId = template.AddTask(stepId, "Task 1", "Task Description", TaskType.Manual, "{}", 1, false);
        DrainEvents(template);
        return (template, stepId, taskId);
    }

    private static void DrainEvents(WorkflowTemplateAggregate template)
    {
        while (template.TryDequeueEvent(out _)) { }
    }

    #endregion
}
