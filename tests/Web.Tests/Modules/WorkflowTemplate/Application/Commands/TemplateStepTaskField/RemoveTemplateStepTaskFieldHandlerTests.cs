using Core.Shared.Errors;
using Core.Shared.Results;
using Moq;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Application.UseCases.Commands.TemplateStepTaskField;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Commands.TemplateStepTaskField;

public class RemoveTemplateStepTaskFieldHandlerTests
{
    private readonly Mock<IWorkflowTemplateApplicationService> _applicationService;
    private readonly RemoveTemplateStepTaskFieldHandler _handler;

    public RemoveTemplateStepTaskFieldHandlerTests()
    {
        _applicationService = new Mock<IWorkflowTemplateApplicationService>();
        _handler = new RemoveTemplateStepTaskFieldHandler(_applicationService.Object);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ReturnsFailure()
    {
        var notFoundError = new NotFoundError(new Error("WorkflowTemplate.NotFound", "Template not found."));
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WorkflowTemplateAggregate>(notFoundError));

        var result = await _handler.Handle(new RemoveTemplateStepTaskFieldCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("WorkflowTemplate.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_ReturnsSuccess()
    {
        var (template, stepId, taskId, fieldId) = CreateTemplateWithStepTaskAndField();
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        var result = await _handler.Handle(new RemoveTemplateStepTaskFieldCommand(Guid.NewGuid(), stepId, taskId, fieldId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_AppendsEvents()
    {
        var (template, stepId, taskId, fieldId) = CreateTemplateWithStepTaskAndField();
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        await _handler.Handle(new RemoveTemplateStepTaskFieldCommand(Guid.NewGuid(), stepId, taskId, fieldId), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStepNotFound_ReturnsStepNotFound()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        var result = await _handler.Handle(new RemoveTemplateStepTaskFieldCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("WorkflowTemplate.StepNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTaskNotFound_ReturnsTaskNotFound()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        var stepId = template.AddStep("Step 1", "Description", 1);
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        var result = await _handler.Handle(new RemoveTemplateStepTaskFieldCommand(Guid.NewGuid(), stepId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("WorkflowTemplate.TaskNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenFieldNotFound_ReturnsTaskFieldNotFound()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        var stepId = template.AddStep("Step 1", "Description", 1);
        var taskId = template.AddTask(stepId, "Task 1", "Description", TaskType.Manual, "{}", 1, false);
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        var result = await _handler.Handle(new RemoveTemplateStepTaskFieldCommand(Guid.NewGuid(), stepId, taskId, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("WorkflowTemplate.TaskFieldNotFound", result.Error.Code);
    }

    private static (WorkflowTemplateAggregate template, Guid stepId, Guid taskId, Guid fieldId) CreateTemplateWithStepTaskAndField()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        var stepId = template.AddStep("Step 1", "Description", 1);
        var taskId = template.AddTask(stepId, "Task 1", "Description", TaskType.Manual, "{}", 1, false);
        var fieldId = template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null, 1);
        return (template, stepId, taskId, fieldId);
    }
}
