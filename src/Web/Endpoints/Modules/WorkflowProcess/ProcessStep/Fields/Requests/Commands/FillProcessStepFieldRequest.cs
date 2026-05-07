using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowProcess.ProcessStep.Fields.Requests.Commands;

public sealed class FillProcessStepFieldBody
{
    public string Value { get; set; } = string.Empty;
}

public sealed class FillProcessStepFieldRequest
{
    [FromRoute(Name = WorkflowProcessRoutes.ProcessId)]
    public Guid ProcessId { get; set; }

    [FromRoute(Name = WorkflowProcessRoutes.StepInstanceId)]
    public Guid StepInstanceId { get; set; }

    [FromRoute(Name = WorkflowProcessRoutes.FieldInstanceId)]
    public Guid FieldInstanceId { get; set; }

    [FromBody]
    public FillProcessStepFieldBody Body { get; set; } = new();
}
