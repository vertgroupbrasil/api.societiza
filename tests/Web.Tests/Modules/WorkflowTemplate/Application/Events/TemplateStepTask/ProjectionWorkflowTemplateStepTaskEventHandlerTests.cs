using Core.Domain.Projection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using WorkflowTemplate.Application.UseCases.Events.TemplateStepTask;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Events.TemplateStepTask;

public class ProjectionWorkflowTemplateStepTaskEventHandlerTests
{
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask>> _taskProjection;
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField>> _taskFieldProjection;
    private readonly Mock<ILogger<ProjectionWorkflowTemplateStepTaskEventHandler>> _logger;
    private readonly ProjectionWorkflowTemplateStepTaskEventHandler _handler;

    public ProjectionWorkflowTemplateStepTaskEventHandlerTests()
    {
        _taskProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask>>();
        _taskFieldProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField>>();
        _logger = new Mock<ILogger<ProjectionWorkflowTemplateStepTaskEventHandler>>();
        _handler = new ProjectionWorkflowTemplateStepTaskEventHandler(
            _taskProjection.Object,
            _taskFieldProjection.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskAdded_CallsReplaceInsertAsync()
    {
        var @event = CreateTaskAddedEvent();

        await _handler.Handle(@event);

        _taskProjection.Verify(x => x.ReplaceInsertAsync(
            It.Is<ProjectionModel.WorkflowTemplateStepTask>(p =>
                p.Id == @event.TaskId &&
                p.TemplateId == @event.TemplateId &&
                p.StepId == @event.StepId &&
                p.Title == @event.Title &&
                p.UpdatedAt == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskAdded_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _taskProjection
            .Setup(x => x.ReplaceInsertAsync(It.IsAny<ProjectionModel.WorkflowTemplateStepTask>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateTaskAddedEvent()));

        Assert.Contains("Failed to create workflow template step task", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskUpdated_CallsUpdateFieldsAsync()
    {
        var @event = CreateTaskUpdatedEvent();

        await _handler.Handle(@event);

        _taskProjection.Verify(x => x.UpdateFieldsAsync(
            @event.TaskId,
            It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplateStepTask>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskUpdated_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _taskProjection
            .Setup(x => x.UpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplateStepTask>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateTaskUpdatedEvent()));

        Assert.Contains("Failed to update workflow template step task", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskRemoved_CallsDeleteAsyncOnTask()
    {
        var @event = CreateTaskRemovedEvent();

        await _handler.Handle(@event);

        _taskProjection.Verify(x => x.DeleteAsync(
            It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStepTask, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskRemoved_WithTaskFieldIds_DeletesCascade()
    {
        var taskFieldId = Guid.NewGuid();
        var @event = CreateTaskRemovedEventWithCascade(taskFieldId);

        await _handler.Handle(@event);

        _taskFieldProjection.Verify(x => x.DeleteAsync(
            It.IsAny<Expression<Func<ProjectionModel.TemplateStepTaskField, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskRemoved_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _taskProjection
            .Setup(x => x.DeleteAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStepTask, bool>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateTaskRemovedEvent()));

        Assert.Contains("Failed to remove workflow template step task", ex.Message);
    }

    private static DomainEvents.WorkflowTemplateStepTaskAdded CreateTaskAddedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        TaskId: Guid.NewGuid(),
        Title: "Task 1",
        Description: "Description",
        Type: "Manual",
        Configuration: "{}",
        Order: 1,
        IsOptional: false,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: 2);

    private static DomainEvents.WorkflowTemplateStepTaskUpdated CreateTaskUpdatedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        TaskId: Guid.NewGuid(),
        Title: "Updated Task",
        Description: "Updated Description",
        Type: "Automatic",
        Configuration: "{}",
        Order: 2,
        IsOptional: true,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: 3);

    private static DomainEvents.WorkflowTemplateStepTaskRemoved CreateTaskRemovedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        TaskId: Guid.NewGuid(),
        TaskFieldIds: [],
        Version: 3);

    private static DomainEvents.WorkflowTemplateStepTaskRemoved CreateTaskRemovedEventWithCascade(Guid taskFieldId) => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        TaskId: Guid.NewGuid(),
        TaskFieldIds: [taskFieldId],
        Version: 3);
}
