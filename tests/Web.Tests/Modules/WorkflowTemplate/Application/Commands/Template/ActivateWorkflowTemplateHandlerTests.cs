using Core.Shared.Errors;
using Core.Shared.Results;
using Moq;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Application.UseCases.Commands.Template;
using WorkflowTemplate.Domain.Enumerations;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Commands.Template;

public class ActivateWorkflowTemplateHandlerTests
{
    private readonly Mock<IWorkflowTemplateApplicationService> _applicationService;
    private readonly ActivateWorkflowTemplateHandler _handler;

    public ActivateWorkflowTemplateHandlerTests()
    {
        _applicationService = new Mock<IWorkflowTemplateApplicationService>();
        _handler = new ActivateWorkflowTemplateHandler(_applicationService.Object);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ReturnsFailure()
    {
        var notFoundError = new NotFoundError(new Error("WorkflowTemplate.NotFound", "Template not found."));
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WorkflowTemplateAggregate>(notFoundError));

        var result = await _handler.Handle(new ActivateWorkflowTemplateCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("WorkflowTemplate.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_ReturnsSuccess()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        var stepId = template.AddStep("Step 1", "Desc", 1, []);
        template.AddTask(stepId, "Task 1", "Desc", TaskType.Manual, "{}", 1, false);
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        var result = await _handler.Handle(new ActivateWorkflowTemplateCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_AppendsEvents()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        var stepId = template.AddStep("Step 1", "Desc", 1, []);
        template.AddTask(stepId, "Task 1", "Desc", TaskType.Manual, "{}", 1, false);
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        await _handler.Handle(new ActivateWorkflowTemplateCommand(Guid.NewGuid()), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
