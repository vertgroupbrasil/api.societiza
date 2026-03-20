using Core.Domain.Projection;
using Microsoft.Extensions.Logging;
using Moq;
using WorkflowTemplate.Application.UseCases.Events;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Events;

public class ProjectionTemplateStepFieldEventHandlerTests
{
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepField>> _projection;
    private readonly Mock<ILogger<ProjectionTemplateStepFieldEventHandler>> _logger;
    private readonly ProjectionTemplateStepFieldEventHandler _handler;

    public ProjectionTemplateStepFieldEventHandlerTests()
    {
        _projection = new Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepField>>();
        _logger = new Mock<ILogger<ProjectionTemplateStepFieldEventHandler>>();
        _handler = new ProjectionTemplateStepFieldEventHandler(_projection.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepFieldAdded_CallsReplaceInsertAsync()
    {
        var @event = CreateFieldAddedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.ReplaceInsertAsync(
            It.Is<ProjectionModel.TemplateStepField>(p =>
                p.Id == @event.FieldId &&
                p.TemplateId == @event.TemplateId &&
                p.StepId == @event.StepId &&
                p.Label == @event.Label &&
                p.UpdatedAt == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepFieldAdded_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.ReplaceInsertAsync(It.IsAny<ProjectionModel.TemplateStepField>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateFieldAddedEvent()));

        Assert.Contains("Failed to create template step field", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepFieldUpdated_CallsUpdateFieldsAsync()
    {
        var @event = CreateFieldUpdatedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.UpdateFieldsAsync(
            @event.FieldId,
            It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.TemplateStepField>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepFieldUpdated_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.UpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.TemplateStepField>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateFieldUpdatedEvent()));

        Assert.Contains("Failed to update template step field", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepFieldRemoved_CallsDeleteAsync()
    {
        var @event = CreateFieldRemovedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.DeleteAsync(@event.FieldId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateStepFieldRemoved_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateFieldRemovedEvent()));

        Assert.Contains("Failed to remove template step field", ex.Message);
    }

    private static DomainEvents.WorkflowTemplateStepFieldAdded CreateFieldAddedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        FieldId: Guid.NewGuid(),
        Label: "Field 1",
        FieldType: "Text",
        Options: null,
        CreatedAt: DateTimeOffset.UtcNow,
        Version: 2);

    private static DomainEvents.WorkflowTemplateStepFieldUpdated CreateFieldUpdatedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        FieldId: Guid.NewGuid(),
        Label: "Updated Field",
        FieldType: "Boolean",
        Options: null,
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: 3);

    private static DomainEvents.WorkflowTemplateStepFieldRemoved CreateFieldRemovedEvent() => new(
        TemplateId: Guid.NewGuid(),
        StepId: Guid.NewGuid(),
        FieldId: Guid.NewGuid(),
        Version: 3);
}
