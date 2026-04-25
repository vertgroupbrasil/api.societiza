namespace Identity.Shared.Response.User;

public sealed record UserProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    Guid? AccountancyId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);