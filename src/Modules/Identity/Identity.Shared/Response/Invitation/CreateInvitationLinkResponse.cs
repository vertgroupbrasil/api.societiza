namespace Identity.Shared.Response.Invitation;

public sealed record CreateInvitationLinkResponse(
    Guid Id,
    string Token,
    string Url,
    DateTimeOffset ExpiresAt
);