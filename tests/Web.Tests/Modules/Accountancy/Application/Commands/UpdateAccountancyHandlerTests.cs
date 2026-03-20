using Accountancy.Application.Services;
using Accountancy.Application.UseCases.Commands;
using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Accountancy.Shared.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Moq;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

namespace Web.Tests.Modules.Accountancy.Application.Commands;

public class UpdateAccountancyHandlerTests
{
    private readonly Mock<IAccountancyApplicationService> _applicationService;
    private readonly Mock<IAccountancyProjection<ProjectionModel.Accountancy>> _accountancyProjection;
    private readonly UpdateAccountancyHandler _handler;

    public UpdateAccountancyHandlerTests()
    {
        _applicationService = new Mock<IAccountancyApplicationService>();
        _accountancyProjection = new Mock<IAccountancyProjection<ProjectionModel.Accountancy>>();
        _handler = new UpdateAccountancyHandler(_applicationService.Object, _accountancyProjection.Object);
    }

    [Fact]
    public async Task Handle_WhenAccountancyNotFound_ReturnsFailure()
    {
        var notFoundError = new NotFoundError(new Error("Accountancy.NotFound", "Accountancy not found."));
        _applicationService
            .Setup(x => x.LoadAggregateAsync<AccountancyAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AccountancyAggregate>(notFoundError));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Accountancy.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenAccountancyExists_ReturnsSuccess()
    {
        var accountancy = AccountancyAggregate.Create(
            cnpj: "12.345.678/0001-95",
            legalName: "Original Name Ltda",
            tradeName: null,
            address: "Rua Original, 1",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310-100",
            phone: "(11) 99999-9999",
            email: null);

        _applicationService
            .Setup(x => x.LoadAggregateAsync<AccountancyAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(accountancy));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenAccountancyExists_AppendsEvents()
    {
        var accountancy = AccountancyAggregate.Create(
            cnpj: "12.345.678/0001-95",
            legalName: "Original Name Ltda",
            tradeName: null,
            address: "Rua Original, 1",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310-100",
            phone: "(11) 99999-9999",
            email: null);

        _applicationService
            .Setup(x => x.LoadAggregateAsync<AccountancyAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(accountancy));

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<AccountancyAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static UpdateAccountancyCommand CreateCommand() => new(
        Id: Guid.NewGuid(),
        CNPJ: "12.345.678/0001-95",
        LegalName: "Updated Accountancy Ltda",
        TradeName: "Updated",
        Address: "Rua Nova, 456",
        City: "Campinas",
        State: "SP",
        PostalCode: "13010-100",
        Phone: "(19) 98888-8888",
        Email: "updated@test.com");
}
