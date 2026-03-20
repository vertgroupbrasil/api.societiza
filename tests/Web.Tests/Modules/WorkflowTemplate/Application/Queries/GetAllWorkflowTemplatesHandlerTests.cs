using Moq;
using System.Linq.Expressions;
using WorkflowTemplate.Application.UseCases.Queries.Template;
using WorkflowTemplate.Domain.Projection;
using WorkflowTemplate.Persistence.Projection;
using WorkflowTemplate.Shared.Queries.Template;

namespace Web.Tests.Modules.WorkflowTemplate.Application.Queries;

public class GetAllWorkflowTemplatesHandlerTests
{
    private readonly Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>> _projectionGateway;
    private readonly GetAllWorkflowTemplatesHandler _handler;

    public GetAllWorkflowTemplatesHandlerTests()
    {
        _projectionGateway = new Mock<IWorkflowTemplateProjection<ProjectionModel.WorkflowTemplate>>();
        _handler = new GetAllWorkflowTemplatesHandler(_projectionGateway.Object);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyNonDeletedTemplates()
    {
        var templates = new List<ProjectionModel.WorkflowTemplate>
        {
            CreateProjection(name: "Alpha Template"),
            CreateProjection(name: "Beta Template"),
        };

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var result = await _handler.Handle(new GetAllWorkflowTemplatesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_ReturnsTemplatesOrderedByNameThenCreatedAt()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var templates = new List<ProjectionModel.WorkflowTemplate>
        {
            CreateProjection(name: "Zeta Template", createdAt: createdAt),
            CreateProjection(name: "Alpha Template", createdAt: createdAt.AddMinutes(1)),
            CreateProjection(name: "Alpha Template", createdAt: createdAt),
        };

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var result = await _handler.Handle(new GetAllWorkflowTemplatesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alpha Template", result.Value[0].Name);
        Assert.Equal(createdAt, result.Value[0].CreatedAt);
        Assert.Equal("Alpha Template", result.Value[1].Name);
        Assert.Equal(createdAt.AddMinutes(1), result.Value[1].CreatedAt);
        Assert.Equal("Zeta Template", result.Value[2].Name);
    }

    [Fact]
    public async Task Handle_WhenNoTemplatesExist_ReturnsEmptyList()
    {
        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetAllWorkflowTemplatesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsMappedResponse()
    {
        var projection = CreateProjection(name: "Test Template");

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.WorkflowTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([projection]);

        var result = await _handler.Handle(new GetAllWorkflowTemplatesQuery(), CancellationToken.None);

        var response = result.Value.Single();
        Assert.Equal(projection.Id, response.Id);
        Assert.Equal(projection.Name, response.Name);
        Assert.Equal(projection.Description, response.Description);
        Assert.Equal(projection.TemplateVersion, response.Version);
        Assert.Equal(projection.Status, response.Status);
        Assert.Equal(projection.CreatedAt, response.CreatedAt);
    }

    private static ProjectionModel.WorkflowTemplate CreateProjection(
        string name = "My Template",
        bool isDeleted = false,
        DateTimeOffset? createdAt = null) => new(
            Id: Guid.NewGuid(),
            Name: name,
            Description: "My Description",
            TemplateVersion: "v1.0",
            Status: "Draft",
            CreatedAt: createdAt ?? DateTimeOffset.UtcNow,
            UpdatedAt: null,
            IsDeleted: isDeleted);
}
