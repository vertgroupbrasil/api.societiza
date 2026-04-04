using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepField.Requests.Commands;

public class UpdateTemplateStepFieldRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid TemplateId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowStepId)]
    public Guid StepId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowFieldId)]
    public Guid FieldId { get; set; }

    [FromBody]
    public UpdateTemplateStepFieldBody Body { get; set; } = null!;
}

public class UpdateTemplateStepFieldBody
{
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string? Options { get; set; }
    public int Order { get; set; }
}