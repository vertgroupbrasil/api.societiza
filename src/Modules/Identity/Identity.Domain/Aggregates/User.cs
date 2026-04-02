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
    public UserRole Role { get; private set; }
    public Guid? AccountancyId { get; private set; }
    public List<RefreshToken> RefreshTokens { get; private set; } = [];
    public List<PasswordResetToken> PasswordResetTokens { get; private set; } = [];

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

    public Guid AddPasswordResetToken(string tokenHash, DateTimeOffset expiresAt)
    {
        var tokenId = Guid.NewGuid();

        RaiseEvent<DomainEvents.UserPasswordResetTokenAdded>(version => new(
            UserId: Id,
            TokenId: tokenId,
            TokenHash: tokenHash,
            ExpiresAt: expiresAt,
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
}
