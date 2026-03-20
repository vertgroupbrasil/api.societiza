using Accountancy.Application.Services;
using Accountancy.Application.UseCases.Commands;
using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Accountancy.Shared.Commands;
using Core.Shared.Errors;
using Moq;
using System.Linq.Expressions;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

namespace Web.Tests.Modules.Accountancy.Application.Commands;

public class CreateAccountancyHandlerTests
{
    private readonly Mock<IAccountancyApplicationService> _applicationService;
    private readonly Mock<IAccountancyProjection<ProjectionModel.Accountancy>> _accountancyProjection;
    private readonly CreateAccountancyHandler _handler;

    public CreateAccountancyHandlerTests()
    {
        _applicationService = new Mock<IAccountancyApplicationService>();
        _accountancyProjection = new Mock<IAccountancyProjection<ProjectionModel.Accountancy>>();
        _handler = new CreateAccountancyHandler(_applicationService.Object, _accountancyProjection.Object);
    }

    [Fact]
    public async Task Handle_WhenCNPJAlreadyExists_ReturnsConflictFailure()
    {
        var existingProjection = new ProjectionModel.Accountancy(
            Id: Guid.NewGuid(),
            CNPJ: "12345678000195",
            LegalName: "Existing Accountancy Ltda",
            TradeName: null,
            Address: "Rua Teste, 123",
            City: "São Paulo",
            State: "SP",
            PostalCode: "01310100",
            Phone: "11999999999",
            Email: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: null);

        _accountancyProjection
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProjection);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.IsType<ConflictError>(result.Error);
        Assert.Equal("CNPJAlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenCNPJDoesNotExist_ReturnsSuccessWithId()
    {
        _accountancyProjection
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectionModel.Accountancy?)null);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public async Task Handle_WhenCNPJDoesNotExist_AppendsEvents()
    {
        _accountancyProjection
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectionModel.Accountancy?)null);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<AccountancyAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateAccountancyCommand CreateCommand() => new(
        CNPJ: "12.345.678/0001-95",
        LegalName: "Test Accountancy Ltda",
        TradeName: "Test",
        Address: "Rua Teste, 123",
        City: "São Paulo",
        State: "SP",
        PostalCode: "01310-100",
        Phone: "(11) 99999-9999",
        Email: "test@test.com");
}
