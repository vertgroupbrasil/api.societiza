using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.Corporate.Accountancy.Requests;

public class GetAccountanciesRequest
{
    [FromQuery(Name = "searchTerm")]
    public string? SearchTerm { get; set; }

    [FromQuery(Name = "state")]
    public string? State { get; set; }

    [FromQuery(Name = "city")]
    public string? City { get; set; }

    [FromQuery(Name = "includeDeleted")]
    public bool IncludeDeleted { get; set; } = false;

    [FromQuery(Name = "includeExpired")]
    public bool IncludeExpired { get; set; } = false;

    [FromQuery(Name = "hasDedicatedDatabase")]
    public bool? HasDedicatedDatabase { get; set; }

    [FromQuery(Name = "pageNumber")]
    public int PageNumber { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 50;
}