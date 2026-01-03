using Core.Domain.Entities;
using Core.Shared.Extensions;
using Corporate.Domain.Events;

namespace Corporate.Domain.Entities;

public class Accountancy : AggregateRoot
{
    public string LegalName { get; private set; } = string.Empty;
    public string? TradeName { get; private set; }
    public string CNPJ { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public virtual Tenant Tenant { get; private set; } = null!;

    private Accountancy() { }

    public static Accountancy Create(
        string tenantId,
        string legalName,
        string cnpj,
        string address,
        string city,
        string state,
        string postalCode,
        string phone,
        string email,
        string? tradeName = null,
        string? notes = null)
    {

        var accountancy = new Accountancy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LegalName = legalName,
            TradeName = tradeName,
            CNPJ = cnpj.OnlyDigits(),
            Address = address,
            City = city,
            State = state.Trim().ToUpperInvariant(),
            PostalCode = postalCode.OnlyDigits(),
            Phone = phone.OnlyDigits(),
            Email = email,
            Notes = notes
        };

        accountancy.AddDomainEvent(new DomainEvents.AccountancyCreated(
            accountancy.Id,
            accountancy.TenantId));

        return accountancy;
    }

    public void Update(
        string legalName,
        string tradeName,
        string cnpj,
        string address,
        string city,
        string state,
        string postalCode,
        string phone,
        string email,
        string? notes = null)
    {
        LegalName = legalName;
        TradeName = tradeName;
        CNPJ = cnpj.OnlyDigits();
        Address = address;
        City = city;
        State = state.Trim().ToUpperInvariant();
        PostalCode = postalCode.OnlyDigits();
        Phone = phone.OnlyDigits();
        Email = email;
        Notes = notes;
    }
}