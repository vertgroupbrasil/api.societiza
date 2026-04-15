using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Queries;

public class GetWorkflowTemplateByIdRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid Id { get; set; }
}
