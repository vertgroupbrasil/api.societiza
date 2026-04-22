namespace Identity.Shared.Response.Invitation;

public sealed record InvitationLinkByIdResponse(
    Guid Id,
    string TargetRole,
    string Status,
    Guid? AccountancyId,
    Guid CreatedByUserId,
    int MaxUses,
    int Uses,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string Url,
    List<InvitationLinkUserResponse> Users
);