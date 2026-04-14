using Core.Shared.Errors;
using Core.Shared.Results;
using Moq;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Application.UseCases.Commands.TemplateStep;
using WorkflowTemplate.Shared.Commands.TemplateStep;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Commands.TemplateStep;

public class RemoveWorkflowTemplateStepHandlerTests
{
    private readonly Mock<IWorkflowTemplateApplicationService> _applicationService;
    private readonly RemoveWorkflowTemplateStepHandler _handler;

    public RemoveWorkflowTemplateStepHandlerTests()
    {
        _applicationService = new Mock<IWorkflowTemplateApplicationService>();
        _handler = new RemoveWorkflowTemplateStepHandler(_applicationService.Object);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ReturnsFailure()
    {
        var notFoundError = new NotFoundError(new Error("WorkflowTemplate.NotFound", "Template not found."));
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WorkflowTemplateAggregate>(notFoundError));

        var result = await _handler.Handle(new RemoveWorkflowTemplateStepCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("WorkflowTemplate.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_ReturnsSuccess()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        var stepId = template.AddStep("Step 1", "Description", 1, []);
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        var result = await _handler.Handle(new RemoveWorkflowTemplateStepCommand(Guid.NewGuid(), stepId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_AppendsEvents()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        var stepId = template.AddStep("Step 1", "Description", 1, []);
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        await _handler.Handle(new RemoveWorkflowTemplateStepCommand(Guid.NewGuid(), stepId), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
