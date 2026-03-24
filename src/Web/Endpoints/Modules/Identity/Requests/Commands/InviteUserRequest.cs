namespace Web.Endpoints.Modules.Identity.Requests.Commands;

public sealed class InviteUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? AccountancyId { get; set; }
}