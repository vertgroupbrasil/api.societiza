using Core.Domain.Projection;
using Microsoft.Extensions.Logging;
using Moq;
using WorkflowTemplate.Application.UseCases.Events;
using WorkflowTemplate.Domain.Events;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Events;

public class ProjectionWorkflowTemplateEventHandlerTests
{
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>> _projection;
    private readonly Mock<ILogger<ProjectionWorkflowTemplateEventHandler>> _logger;
    private readonly ProjectionWorkflowTemplateEventHandler _handler;

    public ProjectionWorkflowTemplateEventHandlerTests()
    {
        _projection = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>>();
        _logger = new Mock<ILogger<ProjectionWorkflowTemplateEventHandler>>();
        _handler = new ProjectionWorkflowTemplateEventHandler(_projection.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateCreated_CallsReplaceInsertAsync()
    {
        var @event = CreateCreatedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.ReplaceInsertAsync(
            It.Is<ProjectionModel.WorkflowTemplate>(p =>
                p.Id == @event.Id &&
                p.Name == @event.Name &&
                p.Description == @event.Description &&
                p.TemplateVersion == @event.TemplateVersion &&
                p.Status == @event.Status &&
                p.CreatedAt == @event.CreatedAt &&
                p.UpdatedAt == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateCreated_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.ReplaceInsertAsync(It.IsAny<ProjectionModel.WorkflowTemplate>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateCreatedEvent()));

        Assert.Contains("Failed to create workflow template", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateUpdated_CallsUpdateFieldsAsync()
    {
        var @event = CreateUpdatedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.UpdateFieldsAsync(
            @event.Id,
            It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplate>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateUpdated_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.UpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplate>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateUpdatedEvent()));

        Assert.Contains("Failed to update workflow template", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateActivated_CallsUpdateFieldsAsync()
    {
        var @event = CreateActivatedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.UpdateFieldsAsync(
            @event.Id,
            It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplate>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateActivated_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.UpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplate>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateActivatedEvent()));

        Assert.Contains("Failed to activate workflow template", ex.Message);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateArchived_CallsUpdateFieldsAsync()
    {
        var @event = CreateArchivedEvent();

        await _handler.Handle(@event);

        _projection.Verify(x => x.UpdateFieldsAsync(
            @event.Id,
            It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplate>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WorkflowTemplateArchived_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _projection
            .Setup(x => x.UpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.WorkflowTemplate>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateArchivedEvent()));

        Assert.Contains("Failed to archive workflow template", ex.Message);
    }

    private static DomainEvents.WorkflowTemplateCreated CreateCreatedEvent() => new(
        Id: Guid.NewGuid(),
        Name: "My Template",
        Description: "My Description",
        TemplateVersion: "v1.0",
        Status: "Draft",
        CreatedAt: DateTimeOffset.UtcNow,
        Version: 1);

    private static DomainEvents.WorkflowTemplateUpdated CreateUpdatedEvent() => new(
        Id: Guid.NewGuid(),
        Name: "Updated Template",
        Description: "Updated Description",
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: 2);

    private static DomainEvents.WorkflowTemplateActivated CreateActivatedEvent() => new(
        Id: Guid.NewGuid(),
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: 2);

    private static DomainEvents.WorkflowTemplateArchived CreateArchivedEvent() => new(
        Id: Guid.NewGuid(),
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: 3);
}
