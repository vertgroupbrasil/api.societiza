using Core.Domain.Projection;
using Microsoft.Extensions.Logging;
using Moq;
using WorkflowTemplate.Application.UseCases.Events.TemplateStepTask;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Events.TemplateStepTask;

public class ProjectionTemplateStepTaskFieldEventHandlerTests
{
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField>> _projection;
    private readonly Mock<ILogger<ProjectionTemplateStepTaskFieldEventHandler>> _logger;
    private readonly ProjectionTemplateStepTaskFieldEventHandler _handler;

    public ProjectionTemplateStepTaskFieldEventHandlerTests()
    {
        _projection = new Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField>>();
        _logger = new Mock<ILogger<ProjectionTemplateStepTaskFieldEventHandler>>();
        _handler = new ProjectionTemplateStepTaskFieldEventHandler(_projection.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskFieldAdded_CallsReplaceInsertAsync()
    {
        var @event = CreateFieldAddedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.ReplaceInsertAsync(
            It.Is<ProjectionModel.TemplateStepTaskField>(p =>
                p.Id == @event.FieldId &&
                p.TemplateId == @event.TemplateId &&
                p.StepId == @event.StepId &&
                p.TaskId == @event.TaskId &&
                p.Label == @event.Label &&
                p.UpdatedAt == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskFieldAdded_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.ReplaceInsertAsync(It.IsAny<ProjectionModel.TemplateStepTaskField>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateFieldAddedEvent()));

        Assert.Contains("Failed to create template step task field", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskFieldUpdated_CallsUpdateFieldsAsync()
    {
        var @event = CreateFieldUpdatedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.UpdateFieldsAsync(
            @event.FieldId,
            It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.TemplateStepTaskField>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskFieldUpdated_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.UpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.TemplateStepTaskField>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateFieldUpdatedEvent()));

        Assert.Contains("Failed to update template step task field", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskFieldRemoved_CallsDeleteAsync()
    {
        var @event = CreateFieldRemovedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.DeleteAsync(@event.FieldId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepTaskFieldRemoved_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateFieldRemovedEvent()));

        Assert.Contains("Failed to remove template step task field", ex.Message);
    }

    private static DomainEvents.WorkflowTemplateStepTaskFieldAdded CreateFieldAddedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        TaskId: Guid.NewGuid(),
        FieldId: Guid.NewGuid(),
        Label: "Field 1",
        FieldType: "Text",
        Options: null,
        Order: 1,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: 2);

    private static DomainEvents.WorkflowTemplateStepTaskFieldUpdated CreateFieldUpdatedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        TaskId: Guid.NewGuid(),
        FieldId: Guid.NewGuid(),
        Label: "Updated Field",
        FieldType: "Boolean",
        Options: null,
        Order: 1,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: 3);

    private static DomainEvents.WorkflowTemplateStepTaskFieldRemoved CreateFieldRemovedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        TaskId: Guid.NewGuid(),
        FieldId: Guid.NewGuid(),
        Version: 3);
}
