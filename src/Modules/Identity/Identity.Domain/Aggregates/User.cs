using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using Identity.Domain.Enumerations;
using Identity.Domain.Events;

namespace Identity.Domain.Aggregates;

public class User : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public Guid? AccountancyId { get; private set; }

    #region User Methods

    public static User Create(string name, string email, string passwordHash, UserRole role, Guid? accountancyId)
    {
        User user = new();

        user.RaiseEvent<DomainEvents.UserCreated>(version => new(
            Id: user.Id,
            Name: name,
            Email: email,
            PasswordHash: passwordHash,
            Role: role,
            AccountancyId: accountancyId,
            CreatedAt: user.CreatedAt,
            Version: version
        ));

        return user;
    }

    public static User Invite(string name, string email, string passwordHash, UserRole role, Guid accountancyId)
    {
        User user = new();

        user.RaiseEvent<DomainEvents.UserInvited>(version => new(
            Id: user.Id,
            Name: name,
            Email: email,
            PasswordHash: passwordHash,
            Role: role,
            AccountancyId: accountancyId,
            Version: version
        ));

        return user;
    }

    public void Update(string name, string email)
        => RaiseEvent<DomainEvents.UserUpdated>(version => new(
            Id: Id,
            Name: name,
            Email: email,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

    public void ChangePassword(string passwordHash)
        => RaiseEvent<DomainEvents.UserPasswordChanged>(version => new(
            Id: Id,
            PasswordHash: passwordHash,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

    public void ChangeRole(UserRole role)
        => RaiseEvent<DomainEvents.UserRoleChanged>(version => new(
            Id: Id,
            Role: role,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

    public void Activate()
        => RaiseEvent<DomainEvents.UserActivated>(version => new(
            Id: Id,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

    public void Deactivate()
        => RaiseEvent<DomainEvents.UserDeactivated>(version => new(
            Id: Id,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

    public void Delete()
        => RaiseEvent<DomainEvents.UserDeleted>(version => new(
            Id: Id,
            Version: version
        ));

    #endregion

    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    #region User Event Handlers

    private void When(DomainEvents.UserCreated @event)
    {
        Id = @event.Id;
        Name = @event.Name;
        Email = @event.Email;
        PasswordHash = @event.PasswordHash;
        Role = @event.Role;
        Status = UserStatus.Active;
        AccountancyId = @event.AccountancyId;
    }

    private void When(DomainEvents.UserInvited @event)
    {
        Id = @event.Id;
        Name = @event.Name;
        Email = @event.Email;
        PasswordHash = @event.PasswordHash;
        Role = @event.Role;
        Status = UserStatus.PendingInvite;
        AccountancyId = @event.AccountancyId;
    }

    private void When(DomainEvents.UserUpdated @event)
    {
        Name = @event.Name;
        Email = @event.Email;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserPasswordChanged @event)
    {
        PasswordHash = @event.PasswordHash;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserRoleChanged @event)
    {
        Role = @event.Role;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserActivated @event)
    {
        Status = UserStatus.Active;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserDeactivated @event)
    {
        Status = UserStatus.Inactive;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserDeleted @event)
        => IsDeleted = true;

    #endregion
}