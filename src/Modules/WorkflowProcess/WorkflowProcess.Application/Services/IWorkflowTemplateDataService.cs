using WorkflowProcess.Shared.Snapshots;

namespace WorkflowProcess.Application.Services;

public interface IWorkflowTemplateDataService
{
    // Retorna null se o template não existir ou não estiver Active.
    // processType é usado para filtrar steps por AppliesTo — só steps
    // aplicáveis ao tipo de processo são incluídos no snapshot.
    Task<(string Version, List<TemplateStepSnapshot> Steps)?> GetActiveTemplateSnapshotAsync(
        Guid templateId, string processType, CancellationToken ct);
}
