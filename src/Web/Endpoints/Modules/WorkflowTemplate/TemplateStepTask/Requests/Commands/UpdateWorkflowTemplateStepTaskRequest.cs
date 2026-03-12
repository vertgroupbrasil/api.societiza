using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Requests.Commands;

public class UpdateWorkflowTemplateStepTaskRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid TemplateId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowStepId)]
    public Guid StepId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTaskId)]
    public Guid TaskId { get; set; }

    [FromBody]
    public UpdateWorkflowTemplateStepTaskBody Body { get; set; } = null!;
}

public class UpdateWorkflowTemplateStepTaskBody
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Configuration { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsOptional { get; set; }
}