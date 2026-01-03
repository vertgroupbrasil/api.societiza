using Core.Domain.Entities;
using Identity.Domain.Enumerations;

namespace Identity.Domain.Entities;

public class User : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set;} = string.Empty;
    public UserRole Role { get; private set; } = UserRole.ReadOnly;
    public ProviderType? AuthProvider { get; private set; }
    public string? OAuthProviderId { get; private set; }
    public DateTime? LastLogin { get; private set; }
    public string? PasswordHash { get; private set; }
    public string FullName => $"{FirstName} {LastName}";

    private User() {}

    public static User Create(string email, string firstName, string lastName, UserRole role, string? configuration)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role
        };

        return user;
    }

    public void Update(string firstName, string lastName, UserRole? role = null)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();

        if (role.HasValue && role.Value != Role)
        {
            Role = role.Value;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}