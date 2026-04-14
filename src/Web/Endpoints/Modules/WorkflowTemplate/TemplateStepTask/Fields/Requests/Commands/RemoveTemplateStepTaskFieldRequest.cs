using Microsoft.AspNetCore.Mvc;
using Web.Endpoints.Routes;

namespace Web.Endpoints.Modules.WorkflowTemplate.TemplateStepTask.Fields.Requests.Commands;

public class RemoveTemplateStepTaskFieldRequest
{
    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTemplateId)]
    public Guid TemplateId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowStepId)]
    public Guid StepId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowTaskId)]
    public Guid TaskId { get; set; }

    [FromRoute(Name = WorkflowTemplateRoutes.WorkflowFieldId)]
    public Guid FieldId { get; set; }
}