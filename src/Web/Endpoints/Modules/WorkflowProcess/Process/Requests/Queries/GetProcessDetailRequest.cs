using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowProcess.Process.Requests.Queries;

public sealed class GetProcessDetailRequest
{
    [FromRoute(Name = WorkflowProcessRoutes.ProcessId)]
    public Guid ProcessId { get; set; }
}