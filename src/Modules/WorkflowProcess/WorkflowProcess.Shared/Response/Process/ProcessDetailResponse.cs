using WorkflowProcess.Shared.Response.ProcessStep;

namespace WorkflowProcess.Shared.Response.Process;

public sealed record ProcessDetailResponse(
    Guid Id,
    Guid AccountancyId,
    Guid TemplateId,
    string TemplateVersion,
    string ProcessType,
    string TargetClient,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    List<ProcessStepInstanceResponse> Steps
);
