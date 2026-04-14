using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStep.Requests.Commands;

public class AddWorkflowTemplateStepRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid TemplateId { get; set; }

    [FromBody]
    public AddWorkflowTemplateStepBody Body { get; set; } = null!;
}

public class AddWorkflowTemplateStepBody
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<string> AppliesTo { get; set; } = [];
}