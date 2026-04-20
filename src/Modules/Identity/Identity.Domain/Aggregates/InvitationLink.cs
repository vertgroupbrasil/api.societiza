using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using Identity.Domain.Enumerations;
using Identity.Domain.Events;

namespace Identity.Domain.Aggregates;

public class InvitationLink : AggregateRoot
{
    public UserRole TargetRole { get; private set; } = default!;
    public Guid? AccountancyId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public int MaxUses { get; private set; }
    public int Uses { get; private set; }
    public InvitationLinkStatus Status { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? RevokedByUserId { get; private set; }

    public static InvitationLink Create(UserRole targetRole, Guid? accountancyId, Guid createdByUserId, string tokenHash, int maxUses)
    {
        var link = new InvitationLink();

        link.RaiseEvent<DomainEvents.InvitationLinkCreated>(version => new(
            Id: link.Id,
            TargetRole: targetRole.Name,
            Status: InvitationLinkStatus.Active.Name,
            AccountancyId: accountancyId,
            CreatedByUserId: createdByUserId,
            TokenHash: tokenHash,
            MaxUses: maxUses,
            ExpiresAt: DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt: link.CreatedAt,
            Version: version
        ));

        return link;
    }

    public void Consume(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        var newUses = Uses + 1;

        RaiseEvent<DomainEvents.InvitationLinkConsumed>(version => new(
            Id: Id,
            Uses: newUses,
            ConsumedByUserId: userId,
            ConsumedAt: now,
            UpdatedAt: now,
            Version: version
        ));

        if (Uses == MaxUses)
        {
            RaiseEvent<DomainEvents.InvitationLinkExhausted>(version => new(
                Id: Id,
                Status: InvitationLinkStatus.Exhausted.Name,
                UpdatedAt: now,
                Version: version
            ));
        }
    }

    public void Expire()
    {
        var now = DateTimeOffset.UtcNow;

        RaiseEvent<DomainEvents.InvitationLinkExpired>(version => new(
            Id: Id,
            Status: InvitationLinkStatus.Expired.Name,
            UpdatedAt: now,
            Version: version
        ));
    }

    public void Revoke(Guid revokedByUserId)
    {
        var now = DateTimeOffset.UtcNow;

        RaiseEvent<DomainEvents.InvitationLinkRevoked>(version => new(
            Id: Id,
            Status: InvitationLinkStatus.Revoked.Name,
            RevokedByUserId: revokedByUserId,
            UpdatedAt: now,
            Version: version
        ));
    }

    public bool IsUsable()
        => Status == InvitationLinkStatus.Active
        && Uses < MaxUses
        && ExpiresAt > DateTimeOffset.UtcNow;

    protected override void ApplyEvent(IDomainEvent @event)
        => When(@event as dynamic);

    private void When(DomainEvents.InvitationLinkCreated @event)
    {
        Id = @event.Id;
        TargetRole = UserRole.FromName(@event.TargetRole);
        Status = InvitationLinkStatus.FromName(@event.Status);
        AccountancyId = @event.AccountancyId;
        CreatedByUserId = @event.CreatedByUserId;
        TokenHash = @event.TokenHash;
        MaxUses = @event.MaxUses;
        Uses = 0;
        ExpiresAt = @event.ExpiresAt;
    }

    private void When(DomainEvents.InvitationLinkConsumed @event)
    {
        Uses = @event.Uses;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.InvitationLinkExhausted @event)
    {
        Status = InvitationLinkStatus.FromName(@event.Status);
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.InvitationLinkExpired @event)
    {
        Status = InvitationLinkStatus.FromName(@event.Status);
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.InvitationLinkRevoked @event)
    {
        Status = InvitationLinkStatus.FromName(@event.Status);
        RevokedByUserId = @event.RevokedByUserId;
        UpdatedAt = @event.UpdatedAt;
    }
}