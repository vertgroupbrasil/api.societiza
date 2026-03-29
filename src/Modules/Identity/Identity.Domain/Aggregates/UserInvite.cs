using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using Identity.Domain.Enumerations;
using Identity.Domain.Events;

namespace Identity.Domain.Aggregates;

public class UserInvite : AggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public Guid? AccountancyId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public InviteStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public static UserInvite Create(string email, UserRole role, Guid? accountancyId, string tokenHash)
    {
        UserInvite invite = new();

        invite.RaiseEvent<DomainEvents.UserInviteCreated>(version => new(
            Id: invite.Id,
            Email: email,
            Role: role,
            AccountancyId: accountancyId,
            TokenHash: tokenHash,
            Status: InviteStatus.Pending,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(72),
            CreatedAt: invite.CreatedAt,
            Version: version
        ));

        return invite;
    }

    public bool IsValid()
        => Status == InviteStatus.Pending && ExpiresAt > DateTimeOffset.UtcNow;

    public void Accept()
        => ChangeStatus(InviteStatus.Accepted);

    public void Revoke()
        => ChangeStatus(InviteStatus.Revoked);

    public void Expire()
        => ChangeStatus(InviteStatus.Expired);

    public void Resend(string newTokenHash)
    {
        RaiseEvent<DomainEvents.UserInviteResent>(version => new(
            Id: Id,
            TokenHash: newTokenHash,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(72),
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    private void ChangeStatus(InviteStatus newStatus)
    {
        var now = DateTimeOffset.UtcNow;

        if (newStatus == InviteStatus.Accepted)
        {
            RaiseEvent<DomainEvents.UserInviteAccepted>(version => new(
                Id: Id,
                Status: newStatus.Name,
                UsedAt: now,
                UpdatedAt: now,
                Version: version
            ));
        }
        else if (newStatus == InviteStatus.Revoked)
        {
            RaiseEvent<DomainEvents.UserInviteRevoked>(version => new(
                Id: Id,
                Status: newStatus.Name,
                UsedAt: now,
                UpdatedAt: now,
                Version: version
            ));
        }
        else if (newStatus == InviteStatus.Expired)
        {
            RaiseEvent<DomainEvents.UserInviteExpired>(version => new(
                Id: Id,
                Status: newStatus.Name,
                UsedAt: now,
                UpdatedAt: now,
                Version: version
            ));
        }
    }

    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    private void When(DomainEvents.UserInviteCreated @event)
    {
        Id = @event.Id;
        Email = @event.Email;
        Role = UserRole.FromName(@event.Role);
        AccountancyId = @event.AccountancyId;
        TokenHash = @event.TokenHash;
        Status = InviteStatus.FromName(@event.Status);
        ExpiresAt = @event.ExpiresAt;
    }

    private void When(DomainEvents.UserInviteResent @event)
    {
        TokenHash = @event.TokenHash;
        ExpiresAt = @event.ExpiresAt;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserInviteAccepted @event)
    {
        Status = InviteStatus.FromName(@event.Status);
        UsedAt = @event.UsedAt;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserInviteRevoked @event)
    {
        Status = InviteStatus.FromName(@event.Status);
        UsedAt = @event.UsedAt;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserInviteExpired @event)
    {
        Status = InviteStatus.FromName(@event.Status);
        UsedAt = @event.UsedAt;
        UpdatedAt = @event.UpdatedAt;
    }
}