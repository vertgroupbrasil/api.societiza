using Core.Domain.Primitives;

namespace Identity.Domain.Entities;

public class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public PasswordResetToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static PasswordResetToken Create(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
        => new(id, userId, tokenHash, expiresAt);

    public void Use(DateTimeOffset usedAt)
    {
        IsUsed = true;
        UsedAt = usedAt;
    }

    public bool IsValid() => !IsUsed && ExpiresAt > DateTimeOffset.UtcNow;
}