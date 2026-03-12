using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Commands;

public class UpdateWorkflowTemplateRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid Id { get; set; }

    [FromBody]
    public UpdateWorkflowTemplateBody Body { get; set; } = null!;
}

public class UpdateWorkflowTemplateBody
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}