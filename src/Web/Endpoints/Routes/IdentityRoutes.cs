namespace Web.Endpoints.Routes;

internal static class IdentityRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/identity";

    internal const string InviteId = "inviteId";

    internal const string InviteUser = $"{BaseUri}/invite";
    internal const string AcceptInvite = $"{BaseUri}/invite/accept";
    internal const string ResendInvite = $"{BaseUri}/invite/{{{InviteId}:guid}}/resend";
    internal const string RevokeInvite = $"{BaseUri}/invite/{{{InviteId}:guid}}/revoke";
}