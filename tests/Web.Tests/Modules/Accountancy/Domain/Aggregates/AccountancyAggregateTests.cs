using Accountancy.Domain.Events;
using Core.Domain.Exceptions;
using Core.Domain.Events.Interfaces;

using AccountancyAggregate = Accountancy.Domain.Aggregates.Accountancy;

namespace Web.Tests.Modules.Accountancy.Domain.Aggregates;

public class AccountancyAggregateTests
{

    [Fact]
    public void Create_SetsIdAndProperties()
    {
        var accountancy = CreateAccountancy();

        Assert.NotEqual(Guid.Empty, accountancy.Id);
        Assert.Equal("Test Accountancy Ltda", accountancy.LegalName);
        Assert.Equal("Test", accountancy.TradeName);
        Assert.Equal("Rua Teste, 123", accountancy.Address);
        Assert.Equal("São Paulo", accountancy.City);
        Assert.Equal("SP", accountancy.State);
        Assert.Equal("test@test.com", accountancy.Email);
    }

    [Fact]
    public void Create_StripsMaskFromCNPJ()
    {
        var accountancy = AccountancyAggregate.Create(
            cnpj: "12.345.678/0001-95",
            legalName: "Test Ltda",
            tradeName: null,
            address: "Rua Teste, 1",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310100",
            phone: "11999999999",
            email: null);

        Assert.Equal("12345678000195", accountancy.CNPJ);
    }

    [Fact]
    public void Create_StripsMaskFromPostalCode()
    {
        var accountancy = AccountancyAggregate.Create(
            cnpj: "12345678000195",
            legalName: "Test Ltda",
            tradeName: null,
            address: "Rua Teste, 1",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310-100",
            phone: "11999999999",
            email: null);

        Assert.Equal("01310100", accountancy.PostalCode);
    }

    [Fact]
    public void Create_StripsMaskFromPhone()
    {
        var accountancy = AccountancyAggregate.Create(
            cnpj: "12345678000195",
            legalName: "Test Ltda",
            tradeName: null,
            address: "Rua Teste, 1",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310100",
            phone: "(11) 99999-9999",
            email: null);

        Assert.Equal("11999999999", accountancy.Phone);
    }

    [Fact]
    public void Create_RaisesAccountancyCreatedEvent()
    {
        var accountancy = CreateAccountancy();

        Assert.True(accountancy.TryDequeueEvent(out IDomainEvent @event));
        Assert.IsType<DomainEvents.AccountancyCreated>(@event);
    }

    [Fact]
    public void Create_SetsVersionToOne()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out var @event);

        Assert.Equal(1UL, accountancy.Version);
        Assert.Equal(1UL, ((DomainEvents.AccountancyCreated)@event).Version);
    }

    [Fact]
    public void Create_IsNotDeleted()
    {
        var accountancy = CreateAccountancy();

        Assert.False(accountancy.IsDeleted);
    }

    [Fact]
    public void Create_AccountancyCreatedEvent_ContainsCorrectData()
    {
        var accountancy = AccountancyAggregate.Create(
            cnpj: "12.345.678/0001-95",
            legalName: "Test Accountancy Ltda",
            tradeName: "Test",
            address: "Rua Teste, 123",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310-100",
            phone: "(11) 99999-9999",
            email: "test@test.com");

        accountancy.TryDequeueEvent(out var domainEvent);
        var @event = Assert.IsType<DomainEvents.AccountancyCreated>(domainEvent);

        Assert.Equal(accountancy.Id, @event.Id);
        Assert.Equal("12345678000195", @event.CNPJ);
        Assert.Equal("Test Accountancy Ltda", @event.LegalName);
        Assert.Equal("01310100", @event.PostalCode);
        Assert.Equal("11999999999", @event.Phone);
    }

    [Fact]
    public void Update_ChangesProperties()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);

        accountancy.Update(
            cnpj: "98765432000100",
            legalName: "Updated Name Ltda",
            tradeName: "Updated",
            address: "Rua Nova, 456",
            city: "Campinas",
            state: "MG",
            postalCode: "13010100",
            phone: "19988888888",
            email: "updated@test.com");

        Assert.Equal("98765432000100", accountancy.CNPJ);
        Assert.Equal("Updated Name Ltda", accountancy.LegalName);
        Assert.Equal("Updated", accountancy.TradeName);
        Assert.Equal("Rua Nova, 456", accountancy.Address);
        Assert.Equal("Campinas", accountancy.City);
        Assert.Equal("MG", accountancy.State);
        Assert.Equal("13010100", accountancy.PostalCode);
        Assert.Equal("19988888888", accountancy.Phone);
        Assert.Equal("updated@test.com", accountancy.Email);
    }

    [Fact]
    public void Update_StripsMasksFromFields()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);

        accountancy.Update(
            cnpj: "98.765.432/0001-00",
            legalName: "Updated Ltda",
            tradeName: null,
            address: "Rua Nova, 1",
            city: "Campinas",
            state: "SP",
            postalCode: "13010-100",
            phone: "(19) 98888-8888",
            email: null);

        Assert.Equal("98765432000100", accountancy.CNPJ);
        Assert.Equal("13010100", accountancy.PostalCode);
        Assert.Equal("19988888888", accountancy.Phone);
    }

    [Fact]
    public void Update_SetsUpdatedAt()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);

        accountancy.Update("12345678000195", "Updated Ltda", null, "Rua Nova, 1", "Campinas", "SP", "13010100", "19988888888", null);

        Assert.NotNull(accountancy.UpdatedAt);
    }

    [Fact]
    public void Update_RaisesAccountancyUpdatedEvent()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);

        accountancy.Update("12345678000195", "Updated Ltda", null, "Rua Nova, 1", "Campinas", "SP", "13010100", "19988888888", null);

        Assert.True(accountancy.TryDequeueEvent(out var @event));
        Assert.IsType<DomainEvents.AccountancyUpdated>(@event);
    }

    [Fact]
    public void Update_IncrementsVersion()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);

        accountancy.Update("12345678000195", "Updated Ltda", null, "Rua Nova, 1", "Campinas", "SP", "13010100", "19988888888", null);

        Assert.Equal(2UL, accountancy.Version);
    }

    [Fact]
    public void Update_WhenDeleted_ThrowsAggregateIsDeletedException()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);
        accountancy.Delete();
        accountancy.TryDequeueEvent(out _);

        Assert.Throws<AggregateIsDeletedException>(() =>
            accountancy.Update("12345678000195", "Updated Ltda", null, "Rua Nova, 1", "Campinas", "SP", "13010100", "19988888888", null));
    }

    [Fact]
    public void Delete_SetsIsDeletedToTrue()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);

        accountancy.Delete();

        Assert.True(accountancy.IsDeleted);
    }

    [Fact]
    public void Delete_RaisesAccountancyDeletedEvent()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);

        accountancy.Delete();

        Assert.True(accountancy.TryDequeueEvent(out var @event));
        Assert.IsType<DomainEvents.AccountancyDeleted>(@event);
    }

    [Fact]
    public void Delete_IncrementsVersion()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);

        accountancy.Delete();

        Assert.Equal(2UL, accountancy.Version);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ThrowsAggregateIsDeletedException()
    {
        var accountancy = CreateAccountancy();
        accountancy.TryDequeueEvent(out _);
        accountancy.Delete();
        accountancy.TryDequeueEvent(out _);

        Assert.Throws<AggregateIsDeletedException>(() => accountancy.Delete());
    }

    private static AccountancyAggregate CreateAccountancy() => AccountancyAggregate.Create(
        cnpj: "12345678000195",
        legalName: "Test Accountancy Ltda",
        tradeName: "Test",
        address: "Rua Teste, 123",
        city: "São Paulo",
        state: "SP",
        postalCode: "01310100",
        phone: "11999999999",
        email: "test@test.com");
}
