namespace Web.Extensions;

internal static class HttpContextExtensions
{
    private const string RefreshTokenCookie = "refresh_token";

    internal static void SetRefreshTokenCookie(this HttpContext context, string token) =>
        context.Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

    internal static void DeleteRefreshTokenCookie(this HttpContext context) =>
        context.Response.Cookies.Delete(RefreshTokenCookie);
}
