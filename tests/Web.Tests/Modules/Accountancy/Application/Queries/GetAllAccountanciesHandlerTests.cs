using Accountancy.Application.UseCases.Queries;
using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Accountancy.Shared.Queries;
using Moq;
using System.Linq.Expressions;

namespace Web.Tests.Modules.Accountancy.Application.Queries;

public class GetAllAccountanciesHandlerTests
{
    private readonly Mock<IAccountancyProjection<ProjectionModel.Accountancy>> _projectionGateway;
    private readonly GetAllAccountanciesHandler _handler;

    public GetAllAccountanciesHandlerTests()
    {
        _projectionGateway = new Mock<IAccountancyProjection<ProjectionModel.Accountancy>>();
        _handler = new GetAllAccountanciesHandler(_projectionGateway.Object);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyNonDeletedAccountancies()
    {
        var accountancies = new List<ProjectionModel.Accountancy>
        {
            CreateProjection(id: Guid.NewGuid(), legalName: "Alpha Ltda", isDeleted: false),
            CreateProjection(id: Guid.NewGuid(), legalName: "Beta Ltda", isDeleted: false),
        };

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountancies);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_ReturnsAccountanciesOrderedByLegalNameThenCreatedAt()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var accountancies = new List<ProjectionModel.Accountancy>
        {
            CreateProjection(id: Guid.NewGuid(), legalName: "Zeta Ltda", createdAt: createdAt),
            CreateProjection(id: Guid.NewGuid(), legalName: "Alpha Ltda", createdAt: createdAt.AddMinutes(1)),
            CreateProjection(id: Guid.NewGuid(), legalName: "Alpha Ltda", createdAt: createdAt),
        };

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountancies);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Alpha Ltda", result.Value[0].LegalName);
        Assert.Equal(createdAt, result.Value[0].CreatedAt);
        Assert.Equal("Alpha Ltda", result.Value[1].LegalName);
        Assert.Equal(createdAt.AddMinutes(1), result.Value[1].CreatedAt);
        Assert.Equal("Zeta Ltda", result.Value[2].LegalName);
    }

    [Fact]
    public async Task Handle_WhenNoAccountanciesExist_ReturnsEmptyList()
    {
        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsMappedResponse()
    {
        var projection = CreateProjection(id: Guid.NewGuid(), legalName: "Test Ltda");

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([projection]);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(), CancellationToken.None);

        var response = result.Value.Single();
        Assert.Equal(projection.Id, response.Id);
        Assert.Equal(projection.CNPJ, response.CNPJ);
        Assert.Equal(projection.LegalName, response.LegalName);
        Assert.Equal(projection.CreatedAt, response.CreatedAt);
    }

    private static ProjectionModel.Accountancy CreateProjection(
        Guid id,
        string legalName,
        bool isDeleted = false,
        DateTimeOffset? createdAt = null) => new(
            Id: id,
            CNPJ: "12345678000195",
            LegalName: legalName,
            TradeName: null,
            Address: "Rua Teste, 123",
            City: "São Paulo",
            State: "SP",
            PostalCode: "01310100",
            Phone: "11999999999",
            Email: null,
            CreatedAt: createdAt ?? DateTimeOffset.UtcNow,
            UpdatedAt: null,
            IsDeleted: isDeleted);
}
