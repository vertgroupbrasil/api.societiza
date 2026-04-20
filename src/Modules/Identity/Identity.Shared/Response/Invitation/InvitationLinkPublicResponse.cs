namespace Identity.Shared.Response.Invitation;

public sealed record InvitationLinkPublicResponse(
    string TargetRole,
    string Status,
    Guid? AccountancyId
);
