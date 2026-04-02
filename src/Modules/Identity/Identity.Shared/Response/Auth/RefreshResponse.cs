namespace Identity.Shared.Response.Auth;

public sealed record RefreshResponse(string AccessToken, string RefreshToken);
