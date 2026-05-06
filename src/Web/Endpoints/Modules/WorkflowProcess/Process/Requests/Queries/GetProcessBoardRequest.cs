using Microsoft.AspNetCore.Mvc;

namespace Web.Endpoints.Modules.WorkflowProcess.Process.Requests.Queries;

public sealed class GetProcessBoardRequest
{
    [FromQuery]
    public Guid AccountancyId { get; set; }

    [FromQuery]
    public string? ProcessType { get; set; }
}