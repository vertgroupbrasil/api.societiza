using Core.Shared.Results;
using Moq;
using WorkflowTemplate.Application.Services;
using WorkflowTemplate.Application.UseCases.Commands.Template;
using WorkflowTemplate.Shared.Commands.Template;

using WorkflowTemplateAggregate = WorkflowTemplate.Domain.Aggregates.WorkflowTemplate;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Commands.Template;

public class CreateWorkflowTemplateHandlerTests
{
    private readonly Mock<IWorkflowTemplateApplicationService> _applicationService;
    private readonly CreateWorkflowTemplateHandler _handler;

    public CreateWorkflowTemplateHandlerTests()
    {
        _applicationService = new Mock<IWorkflowTemplateApplicationService>();
        _handler = new CreateWorkflowTemplateHandler(_applicationService.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithId()
    {
        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public async Task Handle_AppendsEvents()
    {
        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<WorkflowTemplateAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateWorkflowTemplateCommand CreateCommand() => new(
        Name: "My Template",
        Description: "My Description");
}
