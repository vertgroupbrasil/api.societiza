namespace Web.Endpoints.Modules.Identity.Auth.Requests;

public sealed class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}