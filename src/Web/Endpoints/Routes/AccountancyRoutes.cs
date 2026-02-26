namespace Web.Endpoints.Routes;

internal static class AccountancyRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/corporate";

    internal const string AccountancyId = "accountancyId";

    internal const string CreateAccountancy = $"{BaseUri}/accountancy";
    internal const string UpdateAccountancy = $"{BaseUri}/accountancy/{{{AccountancyId}:guid}}";
}