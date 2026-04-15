using Core.Domain.Events.Interfaces;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Domain.Events;

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

        var stepId = template.AddStep("Step 1", "Description", 1, []);

        Assert.NotEqual(Guid.Empty, stepId);
    }

    [Fact]
    public void AddStep_AddsStepToCollection()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.AddStep("Step 1", "Description", 1, []);

        Assert.Single(template.Steps);
        Assert.Equal("Step 1", template.Steps[0].Title);
    }

    [Fact]
    public void AddStep_RaisesWorkflowTemplateStepAddedEvent()
    {
        var template = CreateTemplate();
        DrainEvents(template);

        template.AddStep("Step 1", "Description", 1, []);

        Assert.True(template.TryDequeueEvent(out var @event));
        Assert.IsType<DomainEvents.WorkflowTemplateStepAdded>(@event);
    }

    #endregion

    #region UpdateStep

    [Fact]
    public void UpdateStep_ChangesStepProperties()
    {
        var template = CreateTemplate();
        DrainEvents(template);
        var stepId = template.AddStep("Step 1", "Old Description", 1, []);
        DrainEvents(template);

        template.UpdateStep(stepId, "Updated Step", "New Description", 2, []);

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
        var stepId = template.AddStep("Step 1", "Description", 1, []);
        DrainEvents(template);

        template.UpdateStep(stepId, "Updated Step", "New Description", 2, []);

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
        var stepId = template.AddStep("Step 1", "Description", 1, []);
        DrainEvents(template);

        template.RemoveStep(stepId);

        Assert.Empty(template.Steps);
    }

    [Fact]
    public void RemoveStep_RaisesWorkflowTemplateStepRemovedEvent()
    {
        var template = CreateTemplate();
        DrainEvents(template);
        var stepId = template.AddStep("Step 1", "Description", 1, []);
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
        template.AddStep("Step 1", "Description", 1, []);
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

    #endregion

    #region AddStepField

    [Fact]
    public void AddStepField_ReturnsFieldId()
    {
        var (template, stepId) = CreateTemplateWithStep();

        var fieldId = template.AddStepField(stepId, "Field 1", FieldType.Text, null, 1);

        Assert.NotEqual(Guid.Empty, fieldId);
    }

    [Fact]
    public void AddStepField_AddsFieldToStep()
    {
        var (template, stepId) = CreateTemplateWithStep();

        template.AddStepField(stepId, "Field 1", FieldType.Text, null, 1);

        Assert.Single(template.Steps.First(s => s.Id == stepId).Fields);
    }

    #endregion

    #region AddTaskField

    [Fact]
    public void AddTaskField_ReturnsFieldId()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();

        var fieldId = template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null, 1);

        Assert.NotEqual(Guid.Empty, fieldId);
    }

    #endregion

    #region StepField Order Resolution

    [Fact]
    public void AddStepField_WhenOrderIsFree_PreservesRequestedOrder()
    {
        var (template, stepId) = CreateTemplateWithStep();

        template.AddStepField(stepId, "Field 1", FieldType.Text, null, 3);

        Assert.Equal(3, template.Steps.First(s => s.Id == stepId).Fields[0].Order);
    }

    [Fact]
    public void AddStepField_WhenOrderIsOccupied_ShiftsExistingField()
    {
        var (template, stepId) = CreateTemplateWithStep();
        template.AddStepField(stepId, "Field 1", FieldType.Text, null, 1);
        DrainEvents(template);

        template.AddStepField(stepId, "Field 2", FieldType.Text, null, 1);

        var fields = template.Steps.First(s => s.Id == stepId).Fields;
        var field1 = fields.First(f => f.Label == "Field 1");
        var field2 = fields.First(f => f.Label == "Field 2");
        Assert.Equal(2, field1.Order);
        Assert.Equal(1, field2.Order);
    }

    [Fact]
    public void UpdateStepField_WhenOrderIsFree_PreservesRequestedOrder()
    {
        var (template, stepId) = CreateTemplateWithStep();
        var fieldId = template.AddStepField(stepId, "Field 1", FieldType.Text, null, 1);
        DrainEvents(template);

        template.UpdateStepField(stepId, fieldId, "Field 1", FieldType.Text, null, 5);

        Assert.Equal(5, template.Steps.First(s => s.Id == stepId).Fields[0].Order);
    }

    [Fact]
    public void UpdateStepField_WhenUpdatingToSameOrder_DoesNotShiftSibling()
    {
        var (template, stepId) = CreateTemplateWithStep();
        var field1Id = template.AddStepField(stepId, "Field 1", FieldType.Text, null, 1);
        template.AddStepField(stepId, "Field 2", FieldType.Text, null, 2);
        DrainEvents(template);

        template.UpdateStepField(stepId, field1Id, "Field 1", FieldType.Text, null, 1);

        var fields = template.Steps.First(s => s.Id == stepId).Fields;
        Assert.Equal(1, fields.First(f => f.Label == "Field 1").Order);
        Assert.Equal(2, fields.First(f => f.Label == "Field 2").Order);
    }

    [Fact]
    public void UpdateStepField_WhenOrderIsOccupiedByOtherField_ShiftsOtherField()
    {
        var (template, stepId) = CreateTemplateWithStep();
        var field1Id = template.AddStepField(stepId, "Field 1", FieldType.Text, null, 1);
        template.AddStepField(stepId, "Field 2", FieldType.Text, null, 2);
        DrainEvents(template);

        template.UpdateStepField(stepId, field1Id, "Field 1", FieldType.Text, null, 2);

        var fields = template.Steps.First(s => s.Id == stepId).Fields;
        Assert.Equal(2, fields.First(f => f.Label == "Field 1").Order);
        Assert.Equal(3, fields.First(f => f.Label == "Field 2").Order);
    }

    #endregion

    #region TaskField Order Resolution

    [Fact]
    public void AddTaskField_WhenOrderIsFree_PreservesRequestedOrder()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();

        template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null, 3);

        Assert.Equal(3, template.Steps.First(s => s.Id == stepId).Tasks.First(t => t.Id == taskId).Fields[0].Order);
    }

    [Fact]
    public void AddTaskField_WhenOrderIsOccupied_ShiftsExistingField()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();
        template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null, 1);
        DrainEvents(template);

        template.AddTaskField(stepId, taskId, "Field 2", FieldType.Text, null, 1);

        var fields = template.Steps.First(s => s.Id == stepId).Tasks.First(t => t.Id == taskId).Fields;
        Assert.Equal(2, fields.First(f => f.Label == "Field 1").Order);
        Assert.Equal(1, fields.First(f => f.Label == "Field 2").Order);
    }

    [Fact]
    public void UpdateTaskField_WhenOrderIsFree_PreservesRequestedOrder()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();
        var fieldId = template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null, 1);
        DrainEvents(template);

        template.UpdateTaskField(stepId, taskId, fieldId, "Field 1", FieldType.Text, null, 5);

        Assert.Equal(5, template.Steps.First(s => s.Id == stepId).Tasks.First(t => t.Id == taskId).Fields[0].Order);
    }

    [Fact]
    public void UpdateTaskField_WhenUpdatingToSameOrder_DoesNotShiftSibling()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();
        var field1Id = template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null, 1);
        template.AddTaskField(stepId, taskId, "Field 2", FieldType.Text, null, 2);
        DrainEvents(template);

        template.UpdateTaskField(stepId, taskId, field1Id, "Field 1", FieldType.Text, null, 1);

        var fields = template.Steps.First(s => s.Id == stepId).Tasks.First(t => t.Id == taskId).Fields;
        Assert.Equal(1, fields.First(f => f.Label == "Field 1").Order);
        Assert.Equal(2, fields.First(f => f.Label == "Field 2").Order);
    }

    [Fact]
    public void UpdateTaskField_WhenOrderIsOccupiedByOtherField_ShiftsOtherField()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();
        var field1Id = template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null, 1);
        template.AddTaskField(stepId, taskId, "Field 2", FieldType.Text, null, 2);
        DrainEvents(template);

        template.UpdateTaskField(stepId, taskId, field1Id, "Field 1", FieldType.Text, null, 2);

        var fields = template.Steps.First(s => s.Id == stepId).Tasks.First(t => t.Id == taskId).Fields;
        Assert.Equal(2, fields.First(f => f.Label == "Field 1").Order);
        Assert.Equal(3, fields.First(f => f.Label == "Field 2").Order);
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
        var stepId = template.AddStep("Step 1", "Step Description", 1, []);
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
