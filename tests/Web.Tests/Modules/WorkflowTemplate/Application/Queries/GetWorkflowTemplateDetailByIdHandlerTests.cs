using Core.Shared.Errors;
using Moq;
using System.Linq.Expressions;
using WorkflowTemplate.Application.UseCases.Queries.Template;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;
using WorkflowTemplate.Shared.Queries.Template;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Queries;

public class GetWorkflowTemplateDetailByIdHandlerTests
{
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>> _templateProjection;
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStep>> _stepProjection;
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask>> _taskProjection;
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepField>> _stepFieldProjection;
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField>> _taskFieldProjection;
    private readonly GetWorkflowTemplateDetailByIdHandler _handler;

    public GetWorkflowTemplateDetailByIdHandlerTests()
    {
        _templateProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>>();
        _stepProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStep>>();
        _taskProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplateStepTask>>();
        _stepFieldProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepField>>();
        _taskFieldProjection = new Mock<IWorkflowTemplateProjection<ProjectionModel.TemplateStepTaskField>>();
        _handler = new GetWorkflowTemplateDetailByIdHandler(
            _templateProjection.Object,
            _stepProjection.Object,
            _taskProjection.Object,
            _stepFieldProjection.Object,
            _taskFieldProjection.Object);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ReturnsNotFoundError()
    {
        _templateProjection
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectionModel.WorkflowTemplate?)null);

        var result = await _handler.Handle(new GetWorkflowTemplateDetailByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
        Assert.Equal("WorkflowTemplateNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTemplateFound_ReturnsMappedResponse()
    {
        var templateId = Guid.NewGuid();
        var template = CreateTemplateProjection(templateId);

        SetupProjectionsForTemplate(templateId, template);

        var result = await _handler.Handle(new GetWorkflowTemplateDetailByIdQuery(templateId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(template.Id, result.Value.Id);
        Assert.Equal(template.Name, result.Value.Name);
        Assert.Equal(template.Description, result.Value.Description);
        Assert.Equal(template.TemplateVersion, result.Value.Version);
        Assert.Equal(template.Status, result.Value.Status);
        Assert.Equal(template.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenTemplateFound_ReturnsStepsWithTasks()
    {
        var templateId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var template = CreateTemplateProjection(templateId);
        var step = CreateStepProjection(stepId, templateId, order: 1);
        var task = CreateTaskProjection(taskId, templateId, stepId, order: 1);

        _templateProjection
            .Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _stepProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStep, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([step]);
        _taskProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStepTask, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([task]);
        _stepFieldProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.TemplateStepField, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _taskFieldProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.TemplateStepTaskField, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetWorkflowTemplateDetailByIdQuery(templateId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Steps);
        Assert.Equal(step.Id, result.Value.Steps[0].Id);
        Assert.Equal(step.Title, result.Value.Steps[0].Title);
        Assert.Single(result.Value.Steps[0].Tasks);
        Assert.Equal(task.Id, result.Value.Steps[0].Tasks[0].Id);
    }

    [Fact]
    public async Task Handle_WhenTemplateFound_ReturnsStepsOrderedByOrder()
    {
        var templateId = Guid.NewGuid();
        var template = CreateTemplateProjection(templateId);
        var step1 = CreateStepProjection(Guid.NewGuid(), templateId, order: 2);
        var step2 = CreateStepProjection(Guid.NewGuid(), templateId, order: 1);

        _templateProjection
            .Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _stepProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStep, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([step1, step2]);
        _taskProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStepTask, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _stepFieldProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.TemplateStepField, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _taskFieldProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.TemplateStepTaskField, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetWorkflowTemplateDetailByIdQuery(templateId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Steps[0].Order);
        Assert.Equal(2, result.Value.Steps[1].Order);
    }

    private void SetupProjectionsForTemplate(Guid templateId, ProjectionModel.WorkflowTemplate template)
    {
        _templateProjection
            .Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _stepProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStep, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _taskProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplateStepTask, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _stepFieldProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.TemplateStepField, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _taskFieldProjection
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.TemplateStepTaskField, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static ProjectionModel.WorkflowTemplate CreateTemplateProjection(Guid id) => new(
        Id: id,
        Name: "My Template",
        Description: "My Description",
        TemplateVersion: "v1.0",
        Status: "Draft",
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: null,
        IsDeleted: false);

    private static ProjectionModel.WorkflowTemplateStep CreateStepProjection(Guid stepId, Guid templateId, int order) => new(
        Id: stepId,
        TemplateId: templateId,
        Title: "Step 1",
        Description: "Description",
        Order: order,
        AppliesTo: [],
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: null);

    private static ProjectionModel.WorkflowTemplateStepTask CreateTaskProjection(Guid taskId, Guid templateId, Guid stepId, int order) => new(
        Id: taskId,
        TemplateId: templateId,
        StepId: stepId,
        Title: "Task 1",
        Description: "Description",
        Type: "Manual",
        Configuration: "{}",
        Order: order,
        IsOptional: false,
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: null);
}
