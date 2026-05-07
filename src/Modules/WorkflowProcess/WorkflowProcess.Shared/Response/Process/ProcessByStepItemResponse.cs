namespace WorkflowProcess.Shared.Response.Process;

public sealed record ProcessByStepItemResponse(
    Guid ProcessId,
    Guid StepInstanceId,
    string ProcessType,
    string TargetClient,
    string ProcessStatus,
    string StepStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);
