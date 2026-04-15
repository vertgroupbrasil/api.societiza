using Core.Shared.Errors;
using Core.Shared.Results;
using Moq;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Application.UseCases.Commands.TemplateStep;
using WorkflowTemplate.Shared.Commands.TemplateStep;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Commands.TemplateStep;

public class AddWorkflowTemplateStepHandlerTests
{
    private readonly Mock<IWorkflowTemplateApplicationService> _applicationService;
    private readonly AddWorkflowTemplateStepHandler _handler;

    public AddWorkflowTemplateStepHandlerTests()
    {
        _applicationService = new Mock<IWorkflowTemplateApplicationService>();
        _handler = new AddWorkflowTemplateStepHandler(_applicationService.Object);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ReturnsFailure()
    {
        var notFoundError = new NotFoundError(new Error("WorkflowTemplate.NotFound", "Template not found."));
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WorkflowTemplateAggregate>(notFoundError));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("WorkflowTemplate.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_ReturnsSuccessWithStepId()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public async Task Handle_WhenTemplateExists_AppendsEvents()
    {
        var template = WorkflowTemplateAggregate.Create("My Template", "My Description");
        _applicationService
            .Setup(x => x.LoadAggregateAsync<WorkflowTemplateAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(template));

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AddWorkflowTemplateStepCommand CreateCommand() => new(
        TemplateId: Guid.NewGuid(),
        Title: "Step 1",
        Description: "Step Description",
        Order: 1,
        AppliesTo: []);
}
