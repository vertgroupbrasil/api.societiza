using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowProcess.Process.Requests.Commands;

public sealed class DeleteWorkflowProcessRequest
{
    [FromRoute(Name = WorkflowProcessRoutes.ProcessId)]
    public Guid ProcessId { get; set; }
}
