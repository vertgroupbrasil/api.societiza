using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.Modules.Identity.Invitation.Requests.Queries;

public sealed class GetInvitationLinkByTokenRequest
{
    [FromRoute(Name = "token")]
    public string Token { get; set; } = string.Empty;
}