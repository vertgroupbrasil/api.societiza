using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using Core.Shared.Errors;
using Core.Shared.Results;
using Identity.Domain.Enumerations;
using Identity.Domain.Events;

namespace Identity.Domain.Aggregates;

public class User : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.PendingInvite;
    public Guid? AccountancyId { get; private set; }

    public static User Invite(string name, string email, string passwordHash, UserRole role, Guid? accountancyId)
    {
        User user = new();

        user.RaiseEvent<DomainEvents.UserInvited>(version => new(
            Id: user.Id,
            Name: name,
            Email: email,
            PasswordHash: passwordHash,
            Role: role.ToString(),
            Status: user.Status.ToString(),
            AccountancyId: accountancyId,
            CreatedAt: user.CreatedAt,
            Version: version
        ));

        return user;
    }

    public Result CompleteRegistration(string passwordHash)
    {
        if (Status != UserStatus.PendingInvite)
            return Result.Failure(new ConflictError(new("User.AlreadyActive", "User registration is already completed")));

        RaiseEvent<DomainEvents.UserRegistrationCompleted>(version => new(
            Id: Id,
            PasswordHash: passwordHash,
            Status: UserStatus.Active.ToString(),
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return Result.Success();
    }

    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    private void When(DomainEvents.UserInvited @event)
    {
        Id = @event.Id;
        Name = @event.Name;
        Email = @event.Email;
        PasswordHash = @event.PasswordHash;
        Role = Enum.Parse<UserRole>(@event.Role);
        Status = Enum.Parse<UserStatus>(@event.Status);
        AccountancyId = @event.AccountancyId;
    }

    private void When(DomainEvents.UserRegistrationCompleted @event)
    {
        PasswordHash = @event.PasswordHash;
        Status = Enum.Parse<UserStatus>(@event.Status);
    }
}
