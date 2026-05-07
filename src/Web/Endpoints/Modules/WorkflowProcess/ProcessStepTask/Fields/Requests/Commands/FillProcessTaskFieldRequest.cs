using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowProcess.ProcessStepTask.Fields.Requests.Commands;

public sealed class FillProcessTaskFieldBody
{
    public string Value { get; set; } = string.Empty;
}

public sealed class FillProcessTaskFieldRequest
{
    [FromRoute(Name = WorkflowProcessRoutes.ProcessId)]
    public Guid ProcessId { get; set; }

    [FromRoute(Name = WorkflowProcessRoutes.StepInstanceId)]
    public Guid StepInstanceId { get; set; }

    [FromRoute(Name = WorkflowProcessRoutes.TaskInstanceId)]
    public Guid TaskInstanceId { get; set; }

    [FromRoute(Name = WorkflowProcessRoutes.FieldInstanceId)]
    public Guid FieldInstanceId { get; set; }

    [FromBody]
    public FillProcessTaskFieldBody Body { get; set; } = new();
}
