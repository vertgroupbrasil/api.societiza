namespace Identity.Shared.Response.User;

public sealed record UserDetailResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    DateTimeOffset JoinedAt,
    Guid? InvitationLinkId
);