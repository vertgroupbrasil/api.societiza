using Core.Domain.Projection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using WorkflowTemplate.Application.UseCases.Events;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Events;

public class ProjectionWorkflowTemplateStepEventHandlerTests
{
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStep>> _stepProjection;
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask>> _taskProjection;
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepField>> _stepFieldProjection;
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField>> _taskFieldProjection;
    private readonly Mock<ILogger<ProjectionWorkflowTemplateStepEventHandler>> _logger;
    private readonly ProjectionWorkflowTemplateStepEventHandler _handler;

    public ProjectionWorkflowTemplateStepEventHandlerTests()
    {
        _stepProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStep>>();
        _taskProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask>>();
        _stepFieldProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepField>>();
        _taskFieldProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField>>();
        _logger = new Mock<ILogger<ProjectionWorkflowTemplateStepEventHandler>>();
        _handler = new ProjectionWorkflowTemplateStepEventHandler(
            _stepProjection.Object,
            _taskProjection.Object,
            _stepFieldProjection.Object,
            _taskFieldProjection.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepAdded_CallsReplaceInsertAsync()
    {
        var @event = CreateStepAddedEvent();

        await _handler.Handle(@event);

        _stepProjection.Verify(x => x.ReplaceInsertAsync(
            It.Is<ProjectionModel.WorkflowTemplateStep>(p =>
                p.Id == @event.StepId &&
                p.TemplateId == @event.TemplateId &&
                p.Title == @event.Title &&
                p.UpdatedAt == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepAdded_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _stepProjection
            .Setup(x => x.ReplaceInsertAsync(It.IsAny<ProjectionModel.WorkflowTemplateStep>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateStepAddedEvent()));

        Assert.Contains("Failed to create workflow template step", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepUpdated_CallsUpdateFieldsAsync()
    {
        var @event = CreateStepUpdatedEvent();

        await _handler.Handle(@event);

        _stepProjection.Verify(x => x.UpdateFieldsAsync(
            @event.StepId,
            It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplateStep>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepUpdated_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _stepProjection
            .Setup(x => x.UpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplateStep>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateStepUpdatedEvent()));

        Assert.Contains("Failed to update workflow template step", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepRemoved_CallsDeleteAsyncOnStep()
    {
        var @event = CreateStepRemovedEvent();

        await _handler.Handle(@event);

        _stepProjection.Verify(x => x.DeleteAsync(
            It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStep, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepRemoved_WithCascadeIds_CallsCascadeProjections()
    {
        var fieldId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var taskFieldId = Guid.NewGuid();
        var @event = CreateStepRemovedEventWithCascade(fieldId, taskId, taskFieldId);

        await _handler.Handle(@event);

        _stepFieldProjection.Verify(x => x.DeleteAsync(It.IsAny<Expression<Func<ProjectionModel.TemplateStepField, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _taskProjection.Verify(x => x.DeleteAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStepTask, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _taskFieldProjection.Verify(x => x.DeleteAsync(It.IsAny<Expression<Func<ProjectionModel.TemplateStepTaskField, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepRemoved_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _stepProjection
            .Setup(x => x.DeleteAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStep, bool>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateStepRemovedEvent()));

        Assert.Contains("Failed to remove workflow template step", ex.Message);
    }

    private static DomainEvents.WorkflowTemplateStepAdded CreateStepAddedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        Title: "Step 1",
        Description: "Description",
        Order: 1,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: 2);

    private static DomainEvents.WorkflowTemplateStepUpdated CreateStepUpdatedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        Title: "Updated Step",
        Description: "Updated Description",
        Order: 2,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: 3);

    private static DomainEvents.WorkflowTemplateStepRemoved CreateStepRemovedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        FieldIds: [],
        TaskIds: [],
        TaskFieldIds: [],
        Version: 3);

    private static DomainEvents.WorkflowTemplateStepRemoved CreateStepRemovedEventWithCascade(Guid fieldId, Guid taskId, Guid taskFieldId) => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        FieldIds: [fieldId],
        TaskIds: [taskId],
        TaskFieldIds: [taskFieldId],
        Version: 3);
}
