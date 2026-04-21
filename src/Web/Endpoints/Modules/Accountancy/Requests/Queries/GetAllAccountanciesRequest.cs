using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.Modules.Accountancy.Requests.Queries;

public sealed class GetAllAccountanciesRequest
{
    [FromQuery] public int PageNumber { get; set; } = 1;
    [FromQuery] public int PageSize   { get; set; } = 10;
}