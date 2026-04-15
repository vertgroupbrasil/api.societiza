using WorkflowProcess.Shared.Snapshots;

namespace WorkflowProcess.Application.Services;

public interface IWorkflowTemplateDataService
{
    Task<(string Version, List<TemplateStepSnapshot> Steps)?> GetActiveTemplateSnapshotAsync(Guid templateId, string processType, CancellationToken cancellationToken);
}