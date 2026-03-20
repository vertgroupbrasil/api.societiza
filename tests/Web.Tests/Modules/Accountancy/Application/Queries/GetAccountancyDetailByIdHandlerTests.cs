using Accountancy.Application.UseCases.Queries;
using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Accountancy.Shared.Queries;
using Core.Shared.Errors;
using Moq;

namespace Web.Tests.Modules.Accountancy.Application.Queries;

public class GetAccountancyDetailByIdHandlerTests
{
    private readonly Mock<IAccountancyProjection<ProjectionModel.Accountancy>> _projectionGateway;
    private readonly GetAccountancyDetailByIdHandler _handler;

    public GetAccountancyDetailByIdHandlerTests()
    {
        _projectionGateway = new Mock<IAccountancyProjection<ProjectionModel.Accountancy>>();
        _handler = new GetAccountancyDetailByIdHandler(_projectionGateway.Object);
    }

    [Fact]
    public async Task Handle_WhenAccountancyNotFound_ReturnsNotFoundFailure()
    {
        _projectionGateway
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectionModel.Accountancy?)null);

        var result = await _handler.Handle(new GetAccountancyDetailByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.IsType<NotFoundError>(result.Error);
        Assert.Equal("AccountancyNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenAccountancyExists_ReturnsSuccessWithMappedResponse()
    {
        var projection = CreateProjection();

        _projectionGateway
            .Setup(x => x.GetByIdAsync(projection.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

        var result = await _handler.Handle(new GetAccountancyDetailByIdQuery(projection.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(projection.Id, result.Value.Id);
        Assert.Equal(projection.CNPJ, result.Value.CNPJ);
        Assert.Equal(projection.LegalName, result.Value.LegalName);
        Assert.Equal(projection.TradeName, result.Value.TradeName);
        Assert.Equal(projection.Address, result.Value.Address);
        Assert.Equal(projection.City, result.Value.City);
        Assert.Equal(projection.State, result.Value.State);
        Assert.Equal(projection.PostalCode, result.Value.PostalCode);
        Assert.Equal(projection.Phone, result.Value.Phone);
        Assert.Equal(projection.Email, result.Value.Email);
        Assert.Equal(projection.CreatedAt, result.Value.CreatedAt);
        Assert.Equal(projection.UpdatedAt, result.Value.UpdatedAt);
    }

    private static ProjectionModel.Accountancy CreateProjection() => new(
        Id: Guid.NewGuid(),
        CNPJ: "12345678000195",
        LegalName: "Test Accountancy Ltda",
        TradeName: "Test",
        Address: "Rua Teste, 123",
        City: "São Paulo",
        State: "SP",
        PostalCode: "01310100",
        Phone: "11999999999",
        Email: "test@test.com",
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: null);
}
