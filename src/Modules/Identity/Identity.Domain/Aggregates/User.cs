using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using Identity.Domain.Enumerations;
using Identity.Domain.Events;

namespace Identity.Domain.Aggregates;

public class User : AggregateRoot
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public Guid? AccountancyId { get; private set; }

    public static User Create(Guid inviteId, string firstName, string lastName, string email, string passwordHash, UserRole role, Guid? accountancyId)
    {
        User user = new();

        user.RaiseEvent<DomainEvents.UserRegistrationCompleted>(version => new(
            Id: user.Id,
            InviteId: inviteId,
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            PasswordHash: passwordHash,
            Role: role.Name,
            AccountancyId: accountancyId,
            CreatedAt: user.CreatedAt,
            Version: version
        ));

        return user;
    }

    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    private void When(DomainEvents.UserRegistrationCompleted @event)
    {
        Id = @event.Id;
        FirstName = @event.FirstName;
        LastName = @event.LastName;
        Email = @event.Email;
        PasswordHash = @event.PasswordHash;
        Role = UserRole.FromName(@event.Role);
        AccountancyId = @event.AccountancyId;
    }
}