using Core.Domain.Entities;

namespace Identity.Domain.Entities;

public class RefreshToken : Entity
{
    public string Token { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public bool IsActive => !IsRevoked && !IsExpired;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;

    public virtual User User { get; private set; } = null!;

    private RefreshToken() { }

    public static RefreshToken Create(string token, Guid userId, int expirationDays = 7)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = userId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expirationDays),
        };
    }

    public void Revoke(string? reason = null, string? replacedByToken = null, string? revokedByIp = null)
    {
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedReason = reason;
        ReplacedByToken = replacedByToken;
        RevokedByIp = revokedByIp;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}