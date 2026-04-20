using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.Modules.Identity.Invitation.Requests.Queries;

public sealed class ListInvitationLinksRequest
{
    [FromQuery]
    public bool OnlyActive { get; set; } = true;

    [FromQuery]
    public Guid? AccountancyId { get; set; }
}