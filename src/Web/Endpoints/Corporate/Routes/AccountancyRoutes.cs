namespace Web.Endpoints.Corporate.Routes;

internal static class AccountancyRoutes
{
    private const string Base = "/corporate/accountancy";

    internal const string AccountancyId = "accountancyId";

    internal const string CreateAccountancy = Base;
    internal const string UpdateAccountancy = $"{Base}/{{{AccountancyId}:guid}}";
    internal const string DeleteAccountancy = $"{Base}/{{{AccountancyId}:guid}}";

    internal const string GetAccountancies = Base;
    internal const string GetAccountancyById = $"{Base}/{{{AccountancyId}:guid}}";
}