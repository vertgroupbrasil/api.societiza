namespace Web.Endpoints.Routes;

internal static class AccountancyRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/accountancy";

    internal const string AccountancyId = "accountancyId";

    internal const string CreateAccountancy = $"{BaseUri}";
    internal const string UpdateAccountancy = $"{BaseUri}/{{{AccountancyId}:guid}}";
    internal const string DeleteAccountancy = $"{BaseUri}/{{{AccountancyId}:guid}}";

    internal const string GetAllAccountancies = $"{BaseUri}";
    internal const string GetAccountancyById = $"{BaseUri}/{{{AccountancyId}:guid}}";
}