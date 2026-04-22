namespace Web.Endpoints.Routes;

internal static class IdentityRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/identity";

    // Authentication
    internal const string Login = $"{BaseUri}/auth/login";
    internal const string Logout = $"{BaseUri}/auth/logout";
    internal const string Refresh = $"{BaseUri}/auth/refresh";
    internal const string ForgotPassword = $"{BaseUri}/auth/forgot-password";
    internal const string ResetPassword = $"{BaseUri}/auth/reset-password";

    // Invitation Links
    internal const string InvitationLinkId = "invitationLinkId";

    internal const string CreateInvitationLink    = $"{BaseUri}/invitation-link";
    internal const string ListInvitationLinks     = $"{BaseUri}/invitation-link";
    internal const string GetInvitationLinkById   = $"{BaseUri}/invitation-link/{{{InvitationLinkId}:guid}}";

    internal const string Register = $"{BaseUri}/invitation-link/register";
}