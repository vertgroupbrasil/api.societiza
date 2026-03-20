using Core.Shared.Errors;
using Core.Shared.Results;
using Moq;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Application.UseCases.Commands.TemplateStepTask;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.TemplateStepTask;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Commands.TemplateStepTask;

public class UpdateWorkflowTemplateStepTaskHandlerTests
{
    private readonly Mock<IWorkflowTemplateApplicationService> _applicationService;
    private readonly UpdateWorkflowTemplateStepTaskHandler _handler;

    public UpdateWorkflowTemplateStepTaskHandlerTests()
    {
        _applicationService = new Mock<IWorkflowTemplateApplicationService>();
        _handler = new UpdateWorkflowTemplateStepTaskHandler(_applicationService.Object);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ReturnsFailure()
    {
        var notFoundError = new NotFoundError(new Error("WorkflowTemplate.NotFound", "Template not found."));
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WorkflowTemplateAggregate>(notFoundError));

        var result = await _handler.Handle(CreateCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("WorkflowTemplate.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_ReturnsSuccess()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        var result = await _handler.Handle(CreateCommand(stepId, taskId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_AppendsEvents()
    {
        var (template, stepId, taskId) = CreateTemplateWithStepAndTask();
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        await _handler.Handle(CreateCommand(stepId, taskId), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (WorkflowTemplateAggregate template, Guid stepId, Guid taskId) CreateTemplateWithStepAndTask()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        var stepId = template.AddStep("Step 1", "Description", 1);
        var taskId = template.AddTask(stepId, "Task 1", "Description", TaskType.Manual, "{}", 1, false);
        return (template, stepId, taskId);
    }

    private static UpdateWorkflowTemplateStepTaskCommand CreateCommand(Guid stepId, Guid taskId) => new(
        TemplateId: Guid.NewGuid(),
        StepId: stepId,
        TaskId: taskId,
        Title: "Updated Task",
        Description: "Updated Description",
        Type: "Manual",
        Configuration: "{}",
        Order: 2,
        IsOptional: true);
}
