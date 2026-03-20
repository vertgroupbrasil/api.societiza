using Accountancy.Application.Services;
using Accountancy.Application.UseCases.Commands;
using Accountancy.Shared.Commands;
using Core.Shared.Errors;
using Core.Shared.Results;
using Moq;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

namespace Web.Tests.Modules.Accountancy.Application.Commands;

public class DeleteAccountancyHandlerTests
{
    private readonly Mock<IAccountancyApplicationService> _applicationService;
    private readonly DeleteAccountancyHandler _handler;

    public DeleteAccountancyHandlerTests()
    {
        _applicationService = new Mock<IAccountancyApplicationService>();
        _handler = new DeleteAccountancyHandler(_applicationService.Object);
    }

    [Fact]
    public async Task Handle_WhenAccountancyNotFound_ReturnsFailure()
    {
        var notFoundError = new NotFoundError(new Error("Accountancy.NotFound", "Accountancy not found."));
        _applicationService
            .Setup(x => x.LoadAggregateAsync<AccountancyAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AccountancyAggregate>(notFoundError));

        var result = await _handler.Handle(new DeleteAccountancyCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Accountancy.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenAccountancyExists_ReturnsSuccess()
    {
        var accountancy = AccountancyAggregate.Create(
            cnpj: "12.345.678/0001-95",
            legalName: "Test Accountancy Ltda",
            tradeName: null,
            address: "Rua Teste, 123",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310-100",
            phone: "(11) 99999-9999",
            email: null);

        _applicationService
            .Setup(x => x.LoadAggregateAsync<AccountancyAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(accountancy));

        var result = await _handler.Handle(new DeleteAccountancyCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenAccountancyExists_AppendsEvents()
    {
        var accountancy = AccountancyAggregate.Create(
            cnpj: "12.345.678/0001-95",
            legalName: "Test Accountancy Ltda",
            tradeName: null,
            address: "Rua Teste, 123",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310-100",
            phone: "(11) 99999-9999",
            email: null);

        _applicationService
            .Setup(x => x.LoadAggregateAsync<AccountancyAggregate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(accountancy));

        await _handler.Handle(new DeleteAccountancyCommand(Guid.NewGuid()), CancellationToken.None);

        _applicationService.Verify(x => x.AppendEventsAsync(It.IsAny<AccountancyAggregate>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
