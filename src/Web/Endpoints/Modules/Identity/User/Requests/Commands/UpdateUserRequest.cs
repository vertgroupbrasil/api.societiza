namespace Web.Endpoints.Modules.Identity.User.Requests.Commands;

public sealed class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}