using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Requests.Commands;

public class AddWorkflowTemplateStepTaskRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid TemplateId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowStepId)]
    public Guid StepId { get; set; }

    [FromBody]
    public AddWorkflowTemplateStepTaskBody Body { get; set; } = null!;
}

public class AddWorkflowTemplateStepTaskBody
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Configuration { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsOptional { get; set; }
}