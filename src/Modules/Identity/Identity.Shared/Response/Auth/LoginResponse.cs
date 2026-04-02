namespace Identity.Shared.Response.Auth;

public sealed record LoginResponse(string AccessToken, string RefreshToken);
