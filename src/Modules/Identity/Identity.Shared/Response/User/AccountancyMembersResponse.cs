namespace Identity.Shared.Response.User;

public sealed record AccountancyMembersResponse(
    IReadOnlyList<UserDetailResponse>? Members,
    IReadOnlyList<UserSummaryResponse>? Summaries
);