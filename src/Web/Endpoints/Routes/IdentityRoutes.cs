namespace Web.Endpoints.Routes;

internal static class IdentityRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/identity";

    internal const string InviteUser = $"{BaseUri}/invite";
    internal const string AcceptInvite = $"{BaseUri}/invite/accept";
}