namespace Web.Endpoints.Modules.WorkflowProcess.Process.Requests.Commands;

public sealed class CreateWorkflowProcessRequest
{
    public Guid AccountancyId { get; set; }
    public Guid TemplateId    { get; set; }
    public string ProcessType { get; set; } = string.Empty;
    public string TargetClient { get; set; } = string.Empty;
}