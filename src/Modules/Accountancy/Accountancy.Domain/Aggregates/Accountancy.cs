using Accountancy.Domain.Events;
using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using Core.Shared.Extensions;

namespace Accountancy.Domain.Aggregates;

public class Accountancy : AggregateRoot
{
    public string CNPJ { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public string? TradeName { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }

    public static Accountancy Create(string cnpj, string legalName, string? tradeName, string address, string city, string state, string postalCode, string phone, string? email)
    {
        Accountancy accountancy = new();

        accountancy.RaiseEvent<DomainEvents.AccountancyCreated>(version => new(
            Id: accountancy.Id,
            CNPJ: cnpj.OnlyDigits(),
            LegalName: legalName,
            TradeName: tradeName,
            Address: address,
            City: city,
            State: state,
            PostalCode: postalCode.OnlyDigits(),
            Phone: phone.OnlyDigits(),
            Email: email,
            CreatedAt: accountancy.CreatedAt,
            Version: version
        ));

        return accountancy;
    }

    public void Update(string cnpj, string legalName, string? tradeName, string address, string city, string state, string postalCode, string phone, string? email)
    {
        RaiseEvent<DomainEvents.AccountancyUpdated>(version => new(
            Id: Id,
            CNPJ: cnpj.OnlyDigits(),
            LegalName: legalName,
            TradeName: tradeName,
            Address: address,
            City: city,
            State: state,
            PostalCode: postalCode.OnlyDigits(),
            Phone: phone.OnlyDigits(),
            Email: email,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void Delete()
        => RaiseEvent<DomainEvents.AccountancyDeleted>(version => new(Id: Id, Version: version));

    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    private void When(DomainEvents.AccountancyCreated @event)
    {
        Id = @event.Id;
        CNPJ = @event.CNPJ;
        LegalName = @event.LegalName;
        TradeName = @event.TradeName;
        Address = @event.Address;
        City = @event.City;
        State = @event.State;
        PostalCode = @event.PostalCode;
        Phone = @event.Phone;
        Email = @event.Email;
    }

    private void When(DomainEvents.AccountancyUpdated @event)
    {
        CNPJ = @event.CNPJ;
        LegalName = @event.LegalName;
        TradeName = @event.TradeName;
        Address = @event.Address;
        City = @event.City;
        State = @event.State;
        PostalCode = @event.PostalCode;
        Phone = @event.Phone;
        Email = @event.Email;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.AccountancyDeleted @event)
        => IsDeleted = true;
}