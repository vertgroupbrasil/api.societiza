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
            CreateProjection(id: Guid.NewGuid(), legalName: "Alpha Ltda"),
            CreateProjection(id: Guid.NewGuid(), legalName: "Beta Ltda"),
        };

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountancies);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(1, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
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

        var result = await _handler.Handle(new GetAllAccountanciesQuery(1, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var items = result.Value.Items.ToList();
        Assert.Equal("Alpha Ltda", items[0].LegalName);
        Assert.Equal(createdAt, items[0].CreatedAt);
        Assert.Equal("Alpha Ltda", items[1].LegalName);
        Assert.Equal(createdAt.AddMinutes(1), items[1].CreatedAt);
        Assert.Equal("Zeta Ltda", items[2].LegalName);
    }

    [Fact]
    public async Task Handle_WhenNoAccountanciesExist_ReturnsEmptyPagedResult()
    {
        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(1, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.False(result.Value.Page.HasNextPage);
    }

    [Fact]
    public async Task Handle_ReturnsMappedResponse()
    {
        var projection = CreateProjection(id: Guid.NewGuid(), legalName: "Test Ltda");

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([projection]);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(1, 10), CancellationToken.None);

        var response = result.Value.Items.Single();
        Assert.Equal(projection.Id, response.Id);
        Assert.Equal(projection.CNPJ, response.CNPJ);
        Assert.Equal(projection.LegalName, response.LegalName);
        Assert.Equal(projection.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenItemsExceedPageSize_HasNextPageIsTrue()
    {
        var accountancies = Enumerable.Range(0, 11)
            .Select(i => CreateProjection(Guid.NewGuid(), $"Ltda {i:D2}"))
            .ToList();

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountancies);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(PageNumber: 1, PageSize: 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Page.HasNextPage);
    }

    [Fact]
    public async Task Handle_WhenPageSizeIsOne_ReturnsOnlyCurrentPageItem()
    {
        var accountancies = new List<ProjectionModel.Accountancy>
        {
            CreateProjection(Guid.NewGuid(), "Alpha Ltda"),
            CreateProjection(Guid.NewGuid(), "Beta Ltda"),
            CreateProjection(Guid.NewGuid(), "Gamma Ltda")
        };

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountancies);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(PageNumber: 1, PageSize: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var items = result.Value.Items.ToList();
        Assert.Single(items);
        Assert.Equal("Alpha Ltda", items[0].LegalName);
        Assert.True(result.Value.Page.HasNextPage);
        Assert.False(result.Value.Page.HasPreviousPage);
    }

    [Fact]
    public async Task Handle_WhenNavigatingToSecondPage_ReturnsOnlySecondItem()
    {
        var accountancies = new List<ProjectionModel.Accountancy>
        {
            CreateProjection(Guid.NewGuid(), "Alpha Ltda"),
            CreateProjection(Guid.NewGuid(), "Beta Ltda"),
            CreateProjection(Guid.NewGuid(), "Gamma Ltda")
        };

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountancies);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(PageNumber: 2, PageSize: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var items = result.Value.Items.ToList();
        Assert.Single(items);
        Assert.Equal("Beta Ltda", items[0].LegalName);
        Assert.True(result.Value.Page.HasNextPage);
        Assert.True(result.Value.Page.HasPreviousPage);
    }

    [Fact]
    public async Task Handle_WhenNavigatingToLastPage_ReturnsOnlyLastItem()
    {
        var accountancies = new List<ProjectionModel.Accountancy>
        {
            CreateProjection(Guid.NewGuid(), "Alpha Ltda"),
            CreateProjection(Guid.NewGuid(), "Beta Ltda"),
            CreateProjection(Guid.NewGuid(), "Gamma Ltda")
        };

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountancies);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(PageNumber: 3, PageSize: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var items = result.Value.Items.ToList();
        Assert.Single(items);
        Assert.Equal("Gamma Ltda", items[0].LegalName);
        Assert.False(result.Value.Page.HasNextPage);
        Assert.True(result.Value.Page.HasPreviousPage);
    }

    [Fact]
    public async Task Handle_WhenOnLastPage_HasNextPageIsFalse()
    {
        var accountancies = Enumerable.Range(0, 5)
            .Select(i => CreateProjection(Guid.NewGuid(), $"Ltda {i:D2}"))
            .ToList();

        _projectionGateway
            .Setup(x => x.ListAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountancies);

        var result = await _handler.Handle(new GetAllAccountanciesQuery(PageNumber: 1, PageSize: 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Page.HasNextPage);
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
