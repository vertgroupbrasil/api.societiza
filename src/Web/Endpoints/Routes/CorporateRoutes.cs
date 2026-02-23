namespace Web.Endpoints.Routes;

internal static class CorporateRoutes
{
    internal const string BaseUri = "v{version:apiVersion}/corporate";

    internal const string CreateAccountancy = $"{BaseUri}/accountancy";
}