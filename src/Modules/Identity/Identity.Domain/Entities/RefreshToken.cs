using Core.Domain.Primitives;

namespace Identity.Domain.Entities;

public class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Create(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
        => new(id, userId, tokenHash, expiresAt);

    public void Revoke(DateTimeOffset revokedAt)
    {
        IsRevoked = true;
        RevokedAt = revokedAt;
    }

    public bool IsValid() => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;
}
