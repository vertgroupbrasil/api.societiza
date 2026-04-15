using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Fields.Requests.Commands;

public class UpdateTemplateStepTaskFieldRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid TemplateId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowStepId)]
    public Guid StepId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTaskId)]
    public Guid TaskId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowFieldId)]
    public Guid FieldId { get; set; }

    [FromBody]
    public UpdateTemplateStepTaskFieldBody Body { get; set; } = null!;
}

public class UpdateTemplateStepTaskFieldBody
{
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string? Options { get; set; }
    public int Order { get; set; }
}