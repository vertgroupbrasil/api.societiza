namespace Identity.Shared.Response.Invitation;

public sealed record InvitationLinkDetailResponse(
    Guid Id,
    string TargetRole,
    string Status,
    Guid? AccountancyId,
    Guid CreatedByUserId,
    int MaxUses,
    int Uses,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? UpdatedAt
);
