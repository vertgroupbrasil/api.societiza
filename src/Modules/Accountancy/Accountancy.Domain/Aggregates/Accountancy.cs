using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using Accountancy.Domain.Events;

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
    public string? Email { get; private set; } = string.Empty;

    public static Accountancy Create(string legalName, string cnpj, string? tradeName, string address, string city, string state, string postalCode, string phone, string? email)
    {
        Accountancy accountancy = new();

        accountancy.RaiseEvent<DomainEvents.AccountancyCreated>(version => new(
            Id: accountancy.Id,
            LegalName: legalName,
            CNPJ: cnpj,
            TradeName: tradeName,
            Address: address,
            City: city,
            State: state,
            PostalCode: postalCode,
            Phone: phone,
            Email: email,
            CreatedAt: accountancy.CreatedAt,
            Version: version
        ));

        return accountancy;
    }

    public void Update(string legalName, string cnpj, string? tradeName, string address, string city, string state, string postalCode, string phone, string? email)
    {
        RaiseEvent<DomainEvents.AccountancyUpdated>(version => new(
            Id: Id,
            LegalName: legalName,
            CNPJ: cnpj,
            TradeName: tradeName,
            Address: address,
            City: city,
            State: state,
            PostalCode: postalCode,
            Phone: phone,
            Email: email,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
         ));
    }

    public void Delete()
    {
        RaiseEvent<DomainEvents.AccountancyDeleted>(version => new(
            Id: Id, 
            UpdatedAt: DateTimeOffset.UtcNow,
            version
        ));
    }

    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    private void When(DomainEvents.AccountancyCreated @event)
    {
        Id = @event.Id;
        LegalName = @event.LegalName;
        CNPJ = @event.CNPJ;
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
        LegalName = @event.LegalName;
        CNPJ = @event.CNPJ;
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
    {
        IsDeleted = true;
        UpdatedAt = @event.UpdatedAt;
    }
}