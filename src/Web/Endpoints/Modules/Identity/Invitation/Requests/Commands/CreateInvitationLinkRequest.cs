namespace Web.Endpoints.Modules.Identity.Invitation.Requests.Commands;

public sealed class CreateInvitationLinkRequest
{
    public string TargetRole { get; set; } = string.Empty;
    public Guid? AccountancyId { get; set; }
    public int? MaxUses { get; set; }
}