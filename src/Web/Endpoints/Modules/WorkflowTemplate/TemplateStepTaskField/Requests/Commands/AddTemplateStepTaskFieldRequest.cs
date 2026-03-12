using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTaskField.Requests.Commands;

public class AddTemplateStepTaskFieldRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid TemplateId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowStepId)]
    public Guid StepId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTaskId)]
    public Guid TaskId { get; set; }

    [FromBody]
    public AddTemplateStepTaskFieldBody Body { get; set; } = null!;
}

public class AddTemplateStepTaskFieldBody
{
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string? Options { get; set; }
}