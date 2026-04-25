namespace Web.Endpoints.Modules.Identity.User.Requests.Commands;

public sealed class UpdateMyNameRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
