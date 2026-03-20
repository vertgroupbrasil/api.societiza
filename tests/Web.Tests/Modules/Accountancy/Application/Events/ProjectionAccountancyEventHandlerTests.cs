using Accountancy.Application.UseCases.Events;
using Accountancy.Domain.Events;
using Accountancy.Domain.Projection;
using Accountancy.Persistence.Projection;
using Core.Domain.Projection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Web.Tests.Modules.Accountancy.Application.Events;

public class ProjectionAccountancyEventHandlerTests
{
    private readonly Mock<IAccountancyProjection<ProjectionModel.Accountancy>> _accountancyProjection;
    private readonly Mock<ILogger<ProjectionAccountancyEventHandler>> _logger;
    private readonly ProjectionAccountancyEventHandler _handler;

    public ProjectionAccountancyEventHandlerTests()
    {
        _accountancyProjection = new Mock<IAccountancyProjection<ProjectionModel.Accountancy>>();
        _logger = new Mock<ILogger<ProjectionAccountancyEventHandler>>();
        _handler = new ProjectionAccountancyEventHandler(_accountancyProjection.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_AccountancyCreated_CallsReplaceInsertAsync()
    {
        var @event = CreateAccountancyCreatedEvent();

        await _handler.Handle(@event);

        _accountancyProjection.Verify(x => x.ReplaceInsertAsync(
            It.Is<ProjectionModel.Accountancy>(p =>
                p.Id == @event.Id &&
                p.CNPJ == @event.CNPJ &&
                p.LegalName == @event.LegalName &&
                p.CreatedAt == @event.CreatedAt &&
                p.UpdatedAt == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AccountancyCreated_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _accountancyProjection
            .Setup(x => x.ReplaceInsertAsync(It.IsAny<ProjectionModel.Accountancy>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateAccountancyCreatedEvent()));

        Assert.Contains("Failed to create accountancy", ex.Message);
    }

    [Fact]
    public async Task Handle_AccountancyUpdated_CallsUpdateFieldsAsync()
    {
        var @event = CreateAccountancyUpdatedEvent();

        await _handler.Handle(@event);

        _accountancyProjection.Verify(x => x.UpdateFieldsAsync(
            @event.Id,
            It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.Accountancy>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AccountancyUpdated_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _accountancyProjection
            .Setup(x => x.UpdateFieldsAsync(It.IsAny<Guid>(), It.IsAny<Action<FieldUpdateBuilder<ProjectionModel.Accountancy>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateAccountancyUpdatedEvent()));

        Assert.Contains("Failed to update accountancy", ex.Message);
    }

    [Fact]
    public async Task Handle_AccountancyDeleted_CallsUpdateOneFieldAsync()
    {
        var @event = CreateAccountancyDeletedEvent();

        await _handler.Handle(@event);

        _accountancyProjection.Verify(x => x.UpdateOneFieldAsync(
            @event.Id,
            It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(),
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AccountancyDeleted_WhenProjectionThrows_ThrowsInvalidOperationException()
    {
        _accountancyProjection
            .Setup(x => x.UpdateOneFieldAsync(
                It.IsAny<Guid>(),
                It.IsAny<Expression<Func<ProjectionModel.Accountancy, bool>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(CreateAccountancyDeletedEvent()));

        Assert.Contains("Failed to delete accountancy", ex.Message);
    }

    private static DomainEvents.AccountancyCreated CreateAccountancyCreatedEvent() => new(
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
        Version: 1);

    private static DomainEvents.AccountancyUpdated CreateAccountancyUpdatedEvent() => new(
        Id: Guid.NewGuid(),
        CNPJ: "12345678000195",
        LegalName: "Updated Accountancy Ltda",
        TradeName: "Updated",
        Address: "Rua Nova, 456",
        City: "Campinas",
        State: "SP",
        PostalCode: "13010100",
        Phone: "19988888888",
        Email: "updated@test.com",
        UpdatedAt: DateTimeOffset.UtcNow,
        Version: 2);

    private static DomainEvents.AccountancyDeleted CreateAccountancyDeletedEvent() => new(
        Id: Guid.NewGuid(),
        Version: 3);
}
