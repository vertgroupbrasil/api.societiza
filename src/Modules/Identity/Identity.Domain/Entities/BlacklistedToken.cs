using Core.Domain.Entities;

namespace Identity.Domain.Entities;

public class BlacklistedToken : Entity
{
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset BlacklistedAt { get; set; }
    public string Reason { get; set; } = string.Empty;

    private BlacklistedToken() { }

    public static BlacklistedToken Create(string token, DateTime expiresAt, string reason)
    {
        return new BlacklistedToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            ExpiresAt = expiresAt,
            BlacklistedAt = DateTimeOffset.UtcNow,
            Reason = reason
        };
    }
}