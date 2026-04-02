namespace Web.Endpoints.Modules.Identity.Auth.Requests;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
