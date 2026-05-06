using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowProcess.ProcessStepTask.Requests.Commands;

public sealed class CompleteProcessTaskRequest
{
    [FromRoute(Name = WorkflowProcessRoutes.ProcessId)]
    public Guid ProcessId { get; set; }

    [FromRoute(Name = WorkflowProcessRoutes.StepInstanceId)]
    public Guid StepInstanceId { get; set; }

    [FromRoute(Name = WorkflowProcessRoutes.TaskInstanceId)]
    public Guid TaskInstanceId { get; set; }
}