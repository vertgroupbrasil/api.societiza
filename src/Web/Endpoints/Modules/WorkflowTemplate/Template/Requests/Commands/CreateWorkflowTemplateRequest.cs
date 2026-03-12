namespace Web.Endpoints.Modules.WorkflowTemplate.Template.Requests.Commands;

public sealed class CreateWorkflowTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}