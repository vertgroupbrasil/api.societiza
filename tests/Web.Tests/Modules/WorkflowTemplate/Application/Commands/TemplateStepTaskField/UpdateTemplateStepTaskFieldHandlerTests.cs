using Core.Shared.Errors;
using Core.Shared.Results;
using Moq;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Application.UseCases.Commands.TemplateStepTaskField;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.TemplateStepTaskField;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Commands.TemplateStepTaskField;

public class UpdateTemplateStepTaskFieldHandlerTests
{
    private readonly Mock<IWorkflowTemplateApplicationService> _applicationService;
    private readonly UpdateTemplateStepTaskFieldHandler _handler;

    public UpdateTemplateStepTaskFieldHandlerTests()
    {
        _applicationService = new Mock<IWorkflowTemplateApplicationService>();
        _handler = new UpdateTemplateStepTaskFieldHandler(_applicationService.Object);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ReturnsFailure()
    {
        var notFoundError = new NotFoundError(new Error("WorkflowTemplate.NotFound", "Template not found."));
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WorkflowTemplateAggregate>(notFoundError));

        var result = await _handler.Handle(CreateCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

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

        var result = await _handler.Handle(CreateCommand(stepId, taskId, fieldId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_AppendsEvents()
    {
        var (template, stepId, taskId, fieldId) = CreateTemplateWithStepTaskAndField();
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        await _handler.Handle(CreateCommand(stepId, taskId, fieldId), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (WorkflowTemplateAggregate template, Guid stepId, Guid taskId, Guid fieldId) CreateTemplateWithStepTaskAndField()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        var stepId = template.AddStep("Step 1", "Description", 1);
        var taskId = template.AddTask(stepId, "Task 1", "Description", TaskType.Manual, "{}", 1, false);
        var fieldId = template.AddTaskField(stepId, taskId, "Field 1", FieldType.Text, null);
        return (template, stepId, taskId, fieldId);
    }

    private static UpdateTemplateStepTaskFieldCommand CreateCommand(Guid stepId, Guid taskId, Guid fieldId) => new(
        TemplateId: Guid.NewGuid(),
        StepId: stepId,
        TaskId: taskId,
        FieldId: fieldId,
        Label: "Updated Field",
        FieldType: "Boolean",
        Options: null);
}
