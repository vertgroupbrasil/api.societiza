using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStep.Requests.Commands;

public class UpdateWorkflowTemplateStepRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid TemplateId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowStepId)]
    public Guid StepId { get; set; }

    [FromBody]
    public UpdateWorkflowTemplateStepBody Body { get; set; } = null!;
}

public class UpdateWorkflowTemplateStepBody
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<string> AppliesTo { get; set; } = [];
}