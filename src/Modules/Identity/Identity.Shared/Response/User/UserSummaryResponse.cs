namespace Identity.Shared.Response.User;

public sealed record UserSummaryResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Role
);