namespace Identity.Shared.Response.Invitation;

public sealed record InvitationLinkUserResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    DateTimeOffset JoinedAt
);
