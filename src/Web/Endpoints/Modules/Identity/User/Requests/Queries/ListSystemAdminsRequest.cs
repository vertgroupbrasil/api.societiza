using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.Modules.Identity.User.Requests.Queries;

public sealed class ListSystemAdminsRequest
{
    [FromQuery] public bool OnlyActive { get; set; } = true;
    [FromQuery] public int  PageNumber { get; set; } = 1;
    [FromQuery] public int  PageSize   { get; set; } = 10;
}
