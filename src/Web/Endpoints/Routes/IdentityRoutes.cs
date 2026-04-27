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

    // Users
    internal const string UserId              = "userId";
    internal const string AccountancyIdParam  = "accountancyId";

    internal const string GetMyProfile              = $"{BaseUri}/users";
    internal const string UpdateUser                = $"{BaseUri}/users";
    internal const string ListSystemAdmins          = $"{BaseUri}/system-admins";
    internal const string GetUserById               = $"{BaseUri}/users/{{{UserId}:guid}}";
    internal const string DeleteUser                = $"{BaseUri}/users/{{{UserId}:guid}}";
    internal const string ListAccountancyMembers    = $"{BaseUri}/accountancies/{{{AccountancyIdParam}:guid}}/members";
    internal const string PromoteUser               = $"{BaseUri}/accountancies/{{{AccountancyIdParam}:guid}}/members/{{{UserId}:guid}}/promote";
}