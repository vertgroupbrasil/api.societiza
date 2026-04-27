using Core.Domain.Events.Interfaces;
using Core.Domain.Primitives;
using Identity.Domain.Entities;
using Identity.Domain.Enumerations;
using Identity.Domain.Events;

namespace Identity.Domain.Aggregates;

public class User : AggregateRoot
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public UserRole Role { get; private set; } = default!;
    public Guid? AccountancyId { get; private set; }
    public Guid SecurityStamp { get; private set; }
    public List<RefreshToken> RefreshTokens { get; private set; } = [];
    public List<PasswordResetToken> PasswordResetTokens { get; private set; } = [];

    public static User Create(Guid invitationLinkId, string firstName, string lastName, string email, string passwordHash, UserRole role, Guid? accountancyId)
    {
        User user = new();

        user.RaiseEvent<DomainEvents.UserRegistrationCompleted>(version => new(
            Id: user.Id,
            InvitationLinkId: invitationLinkId,
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            PasswordHash: passwordHash,
            Role: role.Name,
            AccountancyId: accountancyId,
            SecurityStamp: Guid.NewGuid(),
            CreatedAt: user.CreatedAt,
            Version: version
        ));

        return user;
    }

    public Guid AddRefreshToken(string tokenHash, DateTimeOffset expiresAt)
    {
        var tokenId = Guid.NewGuid();

        RaiseEvent<DomainEvents.UserRefreshTokenAdded>(version => new(
            UserId: Id,
            TokenId: tokenId,
            TokenHash: tokenHash,
            ExpiresAt: expiresAt,
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return tokenId;
    }

    public void RevokeRefreshToken(Guid tokenId)
    {
        RaiseEvent<DomainEvents.UserRefreshTokenRevoked>(version => new(
            UserId: Id,
            TokenId: tokenId,
            RevokedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in RefreshTokens.Where(t => t.IsValid()))
            RevokeRefreshToken(token.Id);
    }

    public Guid AddPasswordResetToken(string tokenHash)
    {
        var tokenId = Guid.NewGuid();

        RaiseEvent<DomainEvents.UserPasswordResetTokenAdded>(version => new(
            UserId: Id,
            TokenId: tokenId,
            TokenHash: tokenHash,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));

        return tokenId;
    }

    public void UsePasswordResetToken(Guid tokenId)
    {
        RaiseEvent<DomainEvents.UserPasswordResetTokenUsed>(version => new(
            UserId: Id,
            TokenId: tokenId,
            UsedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void UpdatePassword(string newPasswordHash)
    {
        RaiseEvent<DomainEvents.UserPasswordUpdated>(version => new(
            Id: Id,
            PasswordHash: newPasswordHash,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void RegenerateSecurityStamp()
    {
        RaiseEvent<DomainEvents.UserSecurityStampRegenerated>(version => new(
            UserId: Id,
            SecurityStamp: Guid.NewGuid(),
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void Update(string firstName, string lastName)
    {
        RaiseEvent<DomainEvents.UserUpdated>(version => new(
            UserId: Id,
            FirstName: firstName,
            LastName: lastName,
            UpdatedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void Promote(Guid promotedBy)
    {
        RaiseEvent<DomainEvents.UserPromoted>(version => new(
            UserId: Id,
            AccountancyId: AccountancyId!.Value,
            Role: UserRole.AccountancyAdmin.Name,
            PromotedBy: promotedBy,
            PromotedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
    }

    public void Delete(Guid deletedBy)
    {
        RevokeAllRefreshTokens();
        RegenerateSecurityStamp();

        RaiseEvent<DomainEvents.UserDeleted>(version => new(
            UserId: Id,
            AccountancyId: AccountancyId,
            DeletedBy: deletedBy,
            DeletedAt: DateTimeOffset.UtcNow,
            Version: version
        ));
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
        SecurityStamp = @event.SecurityStamp;
    }

    private void When(DomainEvents.UserRefreshTokenAdded @event)
        => RefreshTokens.Add(RefreshToken.Create(@event.TokenId, @event.UserId, @event.TokenHash, @event.ExpiresAt));

    private void When(DomainEvents.UserRefreshTokenRevoked @event)
    {
        var token = RefreshTokens.FirstOrDefault(t => t.Id == @event.TokenId);
        token?.Revoke(@event.RevokedAt);
    }

    private void When(DomainEvents.UserPasswordResetTokenAdded @event)
        => PasswordResetTokens.Add(PasswordResetToken.Create(@event.TokenId, @event.UserId, @event.TokenHash, @event.ExpiresAt));

    private void When(DomainEvents.UserPasswordResetTokenUsed @event)
    {
        var token = PasswordResetTokens.FirstOrDefault(t => t.Id == @event.TokenId);
        token?.Use(@event.UsedAt);
    }

    private void When(DomainEvents.UserPasswordUpdated @event)
    {
        PasswordHash = @event.PasswordHash;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserSecurityStampRegenerated @event)
    {
        SecurityStamp = @event.SecurityStamp;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserUpdated @event)
    {
        FirstName = @event.FirstName;
        LastName = @event.LastName;
        UpdatedAt = @event.UpdatedAt;
    }

    private void When(DomainEvents.UserPromoted @event)
    {
        Role = UserRole.FromName(@event.Role);
        UpdatedAt = @event.PromotedAt;
    }

    private void When(DomainEvents.UserDeleted @event)
    {
        IsDeleted = true;
        UpdatedAt = @event.DeletedAt;
    }
}
